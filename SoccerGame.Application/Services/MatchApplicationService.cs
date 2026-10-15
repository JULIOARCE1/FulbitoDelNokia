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

    private const float BallControlDistance = 14f;
    private const float StealDistance = 12f;

    private void UpdateEnemyAI(Match match, float deltaTime)
    {
        var awayPlayers = match.AwayTeam.Players;
        if (awayPlayers.Count == 0) return;
        var possessor = FindPlayer(match, match.Ball.PossessorId);

        if (possessor != null && awayPlayers.Any(p => p.Id == possessor.Id))
        {
            var teammate = awayPlayers.Where(p => p.Id != possessor.Id && p.Position.X < possessor.Position.X - 35f)
                .OrderBy(p => p.Position.DistanceTo(possessor.Position)).FirstOrDefault();
            float distanceToGoal = MathF.Abs(possessor.Position.X + 350f);
            if (distanceToGoal < 180f)
                KickTowards(match, possessor, new Position(-350f, 0f), 55f);
            else if (teammate != null && distanceToGoal > 220f && (int)(match.ElapsedTimeInSeconds * 2f) % 9 == 0 && deltaTime > 0f)
                KickTowards(match, possessor, teammate.Position, 42f);
            else
                MoveTowards(match, possessor, new Position(-300f, 0f), deltaTime, 0.72f);
            return;
        }

        var closestEnemy = awayPlayers.OrderBy(p => p.Position.DistanceTo(match.Ball.Position)).First();
        if (!match.Ball.PossessorId.HasValue && closestEnemy.Position.DistanceTo(match.Ball.Position) <= BallControlDistance && BallSpeed(match) < 22f)
        {
            match.Ball.GiveTo(closestEnemy.Id, BallPositionFor(closestEnemy, match));
            return;
        }
        MoveTowards(match, closestEnemy, match.Ball.Position, deltaTime, 0.72f);
    }

    private static void MoveTowards(Match match, Player player, Position target, float deltaTime, float speedFactor)
    {
        float dx = target.X - player.Position.X, dy = target.Y - player.Position.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 1f) return;
        float distance = player.Stats.Speed * speedFactor * deltaTime;
        player.MoveTo(ClampToPitch(match, player.Position.MoveBy(dx / length * distance, dy / length * distance)));
    }

    private void KickTowards(Match match, Player player, Position target, float power)
    {
        float dx = target.X - match.Ball.Position.X, dy = target.Y - match.Ball.Position.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0f) return;
        match.Ball.ApplyImpulse(dx / length * power, dy / length * power);
        _soundService?.PlayKickSound();
    }

    private void CheckPlayerBallCollisions(Match match)
    {
        var allPlayers = match.HomeTeam.Players.Concat(match.AwayTeam.Players).ToList();
        var possessor = FindPlayer(match, match.Ball.PossessorId);
        if (possessor != null)
        {
            var opponent = allPlayers.Where(p => !SameTeam(match, p, possessor))
                .OrderBy(p => p.Position.DistanceTo(possessor.Position)).FirstOrDefault();
            if (opponent != null && opponent.Position.DistanceTo(possessor.Position) < StealDistance)
            {
                match.Ball.GiveTo(opponent.Id, BallPositionFor(opponent, match));
                return;
            }
            match.Ball.GiveTo(possessor.Id, BallPositionFor(possessor, match));
            return;
        }

        if (BallSpeed(match) > 18f) return;
        var nearest = allPlayers.OrderBy(p => p.Position.DistanceTo(match.Ball.Position)).FirstOrDefault();
        if (nearest != null && nearest.Position.DistanceTo(match.Ball.Position) <= BallControlDistance)
            match.Ball.GiveTo(nearest.Id, BallPositionFor(nearest, match));
    }

    private static float BallSpeed(Match match) => MathF.Sqrt(match.Ball.Velocity.X * match.Ball.Velocity.X + match.Ball.Velocity.Y * match.Ball.Velocity.Y);

    private static Position BallPositionFor(Player player, Match match)
    {
        float offsetX = match.HomeTeam.Players.Any(p => p.Id == player.Id) ? 12f : -12f;
        return ClampToPitch(match, player.Position.MoveBy(offsetX, 0f));
    }

    private static Position ClampToPitch(Match match, Position position)
    {
        float halfWidth = match.PitchWidth / 2f - 12f, halfHeight = match.PitchHeight / 2f - 12f;
        return new Position(Math.Clamp(position.X, -halfWidth, halfWidth), Math.Clamp(position.Y, -halfHeight, halfHeight));
    }

    private static bool SameTeam(Match match, Player first, Player second)
    {
        bool firstHome = match.HomeTeam.Players.Any(p => p.Id == first.Id);
        bool secondHome = match.HomeTeam.Players.Any(p => p.Id == second.Id);
        return firstHome == secondHome;
    }

    private static Player? FindPlayer(Match match, Guid? playerId)
    {
        if (!playerId.HasValue) return null;
        return match.HomeTeam.Players.Concat(match.AwayTeam.Players).FirstOrDefault(p => p.Id == playerId.Value);
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

        player.MoveTo(ClampToPitch(match, new Position(newX, newY)));
    }

    public void KickBall(KickBallCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        if (match.State != MatchState.InPlay) return;
        var player = FindPlayer(match, command.PlayerId);
        if (player == null)
            player = match.HomeTeam.Players.OrderBy(p => p.Position.DistanceTo(match.Ball.Position)).FirstOrDefault();
        if (player == null || player.Position.DistanceTo(match.Ball.Position) > BallControlDistance + 8f) return;
        if (match.Ball.PossessorId.HasValue && match.Ball.PossessorId.Value != player.Id) return;

        float dirX = float.IsNaN(command.DirectionX) || float.IsInfinity(command.DirectionX) ? 0f : command.DirectionX;
        float dirY = float.IsNaN(command.DirectionY) || float.IsInfinity(command.DirectionY) ? 0f : command.DirectionY;
        float length = MathF.Sqrt(dirX * dirX + dirY * dirY);
        if (length < 0.001f) return;
        float power = Math.Clamp(float.IsNaN(command.Power) || float.IsInfinity(command.Power) ? 10f : command.Power, 10f, 100f);
        match.Ball.ApplyImpulse(dirX / length * power, dirY / length * power);
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