using System;
using System.Collections.Generic;
using System.Text;

using SoccerGame.Application.Commands;
using SoccerGame.Application.DTOs;
using SoccerGame.Domain.Entities;
using SoccerGame.Domain.ValueObjects;

namespace SoccerGame.Application.Services;

public class MatchApplicationService
{
    private readonly Dictionary<Guid, Match> _activeMatches = new();

    public Guid CreateMatch(string homeTeamName, string awayTeamName, float durationSeconds = 180f)
    {
        var home = new Team(Guid.NewGuid(), homeTeamName);
        var away = new Team(Guid.NewGuid(), awayTeamName);

        // Crear 3 jugadores básicos por equipo para la cancha retro
        home.AddPlayer(new Player(Guid.NewGuid(), "Delantero H", 9, new Position(-50f, 0f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Medio H", 8, new Position(-150f, -50f), PlayerStats.Default));
        home.AddPlayer(new Player(Guid.NewGuid(), "Defensa H", 2, new Position(-250f, 50f), PlayerStats.Default));

        away.AddPlayer(new Player(Guid.NewGuid(), "Delantero A", 11, new Position(50f, 0f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Medio A", 10, new Position(150f, 50f), PlayerStats.Default));
        away.AddPlayer(new Player(Guid.NewGuid(), "Defensa A", 3, new Position(250f, -50f), PlayerStats.Default));

        var ball = new Ball(Position.Zero);
        var match = new Match(Guid.NewGuid(), home, away, ball, durationSeconds);

        _activeMatches[match.Id] = match;
        return match.Id;
    }

    public void StartMatch(StartMatchCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);
        match.Start();
    }

    public void UpdateTick(UpdateTickCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);

        if (match.State == MatchState.GoalScored)
        {
            // Reanudar automáticamente tras 2 segundos de festejo de gol
            match.ResumePlay();
        }

        match.UpdateTick(command.SanitizedDeltaTime);
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

        // Validar dirección para evitar NaN/Infinito
        float dirX = float.IsNaN(command.DirectionX) ? 0f : command.DirectionX;
        float dirY = float.IsNaN(command.DirectionY) ? 0f : command.DirectionY;
        float power = Math.Clamp(command.Power, 10f, 100f);

        match.Ball.ApplyImpulse(dirX * power, dirY * power);
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