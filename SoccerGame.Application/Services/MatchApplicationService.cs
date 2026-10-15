using System;
using System.Collections.Generic;
using System.Text;

using SoccerGame.Application.Commands;
using SoccerGame.Application.DTOs;
using SoccerGame.Application.Interfaces;
using SoccerGame.Domain.Entities;
using SoccerGame.Domain.ValueObjects;

namespace SoccerGame.Application.Services;

public class MatchApplicationService
{
    private readonly Dictionary<Guid, Match> _activeMatches = new();
    private readonly ISoundService? _soundService;
    private readonly IMatchRepository? _matchRepository;

    public MatchApplicationService(ISoundService? soundService = null, IMatchRepository? matchRepository = null)
    {
        _soundService = soundService;
        _matchRepository = matchRepository;
    }

    public Guid CreateMatch(string homeTeamName, string awayTeamName, float durationSeconds = 180f)
    {
        var home = new Team(Guid.NewGuid(), homeTeamName);
        var away = new Team(Guid.NewGuid(), awayTeamName);

        // Alineación Local (3 jugadores)
        home.AddPlayer(new Player(Guid.NewGuid(), "Delantero H", 9, new Position(-50f, 0f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Medio H", 8, new Position(-150f, -60f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Defensa H", 2, new Position(-250f, 60f), PlayerStats.Default));

        // Alineación Visitante (3 jugadores)
        away.AddPlayer(new Player(Guid.NewGuid(), "Delantero A", 11, new Position(50f, 0f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Medio A", 10, new Position(150f, 60f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Defensa A", 3, new Position(250f, -60f), PlayerStats.Default));

        var ball = new Ball(Position.Zero);
        var match = new Match(Guid.NewGuid(), home, away, ball, durationSeconds);

        _activeMatches[match.Id] = match;
        return match.Id;
    }

    public void StartMatch(StartMatchCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        match.Start();
        _soundService?.PlayStartWhistleSound();
    }

    public void UpdateTick(UpdateTickCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        var previousState = match.State;

        if (match.State == MatchState.GoalScored)
        {
            match.ResumePlay();
        }

        match.UpdateTick(command.SanitizedDeltaTime);

        if (match.State == MatchState.InPlay)
        {
            // --- A. IA DEL EQUIPO RIVAL (VISITANTE) ---
            UpdateEnemyAI(match, command.SanitizedDeltaTime);

            // --- B. COLISIÓN Y POSESIÓN JUGADOR-PELOTA ---
            CheckPlayerBallCollisions(match);
        }

        if (previousState == MatchState.InPlay && match.State == MatchState.GoalScored)
        {
            _soundService?.PlayGoalWhistleSound();
        }

        if (previousState == MatchState.InPlay && match.State == MatchState.Finished)
        {
            _soundService?.PlayStartWhistleSound();
            var status = GetMatchStatus(match.Id);
            if (status != null && _matchRepository != null)
            {
                _ = _matchRepository.SaveMatchHistoryAsync(status);
            }
        }
    }

    private void UpdateEnemyAI(Match match, float deltaTime)
    {
        // El jugador visitante más cercano a la pelota toma la iniciativa
        var closestEnemy = match.AwayTeam.Players
            .OrderBy(p => p.Position.DistanceTo(match.Ball.Position))
            .FirstOrDefault();

        if (closestEnemy is null) return;

        float distToBall = closestEnemy.Position.DistanceTo(match.Ball.Position);

        // Si está lejos de la pelota, va directo hacia ella
        if (distToBall > 15f)
        {
            float dx = match.Ball.Position.X - closestEnemy.Position.X;
            float dy = match.Ball.Position.Y - closestEnemy.Position.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);

            if (length > 0)
            {
                float dirX = dx / length;
                float dirY = dy / length;
                float speed = closestEnemy.Stats.Speed * 0.7f; // Ligeramente más lento que el jugador humano
                closestEnemy.MoveTo(closestEnemy.Position.MoveBy(dirX * speed, dirY * speed));
            }
        }
        else
        {
            // Si está al lado de la pelota, patea hacia el arco local (Arco izquierdo: X = -350)
            float dx = -350f - match.Ball.Position.X;
            float dy = 0f - match.Ball.Position.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);

            if (length > 0)
            {
                match.Ball.ApplyImpulse((dx / length) * 45f, (dy / length) * 45f);
                _soundService?.PlayKickSound();
            }
        }
    }

    private void CheckPlayerBallCollisions(Match match)
    {
        // Si un jugador camina sobre la pelota en movimiento, la empuja/conduce levemente
        foreach (var player in match.HomeTeam.Players.Concat(match.AwayTeam.Players))
        {
            float dist = player.Position.DistanceTo(match.Ball.Position);
            if (dist < 12f)
            {
                // Conducción de pelota
                float ballSpeed = MathF.Sqrt(match.Ball.Velocity.X * match.Ball.Velocity.X + match.Ball.Velocity.Y * match.Ball.Velocity.Y);
                if (ballSpeed < 5f)
                {
                    match.Ball.SetPosition(player.Position.MoveBy(8f, 0f));
                }
            }
        }
    }

    public void MovePlayer(MovePlayerCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        if (match.State != MatchState.InPlay) return;

        var player = match.HomeTeam.Players.FirstOrDefault(p => p.Id == command.PlayerId)
                     ?? match.AwayTeam.Players.FirstOrDefault(p => p.Id == command.PlayerId);

        if (player is null) return;

        float speed = player.Stats.Speed;
        float newX = player.Position.X + (command.SanitizedDeltaX * speed);
        float newY = player.Position.Y + (command.SanitizedDeltaY * speed);

        player.MoveTo(new Position(newX, newY));
    }

    public void KickBall(KickBallCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        if (match.State != MatchState.InPlay) return;

        float dirX = float.IsNaN(command.DirectionX) ? 0f : command.DirectionX;
        float dirY = float.IsNaN(command.DirectionY) ? 0f : command.DirectionY;
        float power = Math.Clamp(command.Power, 10f, 100f);

        match.Ball.ApplyImpulse(dirX * power, dirY * power);
        _soundService?.PlayKickSound();
    }

    public MatchStatusDto? GetMatchStatus(Guid matchId)
    {
        if (!_activeMatches.TryGetValue(matchId, out var match))
            return null;

        var playersDto = new List<PlayerDto>();

        foreach (var p in match.HomeTeam.Players)
            playersDto.Add(new PlayerDto(p.Id, p.Name, p.ShirtNumber, p.Position.X, p.Position.Y, IsHomeTeam: true));

        foreach (var p in match.AwayTeam.Players)
            playersDto.Add(new PlayerDto(p.Id, p.Name, p.ShirtNumber, p.Position.X, p.Position.Y, IsHomeTeam: false));

        return new MatchStatusDto(
            match.Id,
            match.HomeTeam.Name,
            match.AwayTeam.Name,
            match.Score.HomeGoals,
            match.Score.AwayGoals,
            match.State.ToString(),
            match.ElapsedTimeInSeconds,
            match.Ball.Position.X,
            match.Ball.Position.Y,
            playersDto.AsReadOnly()
        );
    }

    private Match GetMatchOrThrow(Guid matchId)
    {
        if (!_activeMatches.TryGetValue(matchId, out var match))
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        return match;
    }
}