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
        match.UpdateTick(command.SanitizedDeltaTime);
    }

    public void MovePlayer(MovePlayerCommand command)
    {
        var match = GetMatchOrThrow(command.MatchId);

        if (match.State != MatchState.InPlay) return;

        // Buscar al jugador en ambos equipos
        var player = match.HomeTeam.Players.FirstOrDefault(p => p.Id == command.PlayerId)
                     ?? match.AwayTeam.Players.FirstOrDefault(p => p.Id == command.PlayerId);

        if (player is null) return;

        // Calcular nueva posición respetando estadísticas de velocidad
        float speed = player.Stats.Speed;
        float newX = player.Position.X + (command.SanitizedDeltaX * speed);
        float newY = player.Position.Y + (command.SanitizedDeltaY * speed);

        player.MoveTo(new Position(newX, newY));
    }

    public MatchStatusDto? GetMatchStatus(Guid matchId)
    {
        if (!_activeMatches.TryGetValue(matchId, out var match))
            return null;

        return new MatchStatusDto(
            match.Id,
            match.HomeTeam.Name,
            match.AwayTeam.Name,
            match.Score.HomeGoals,
            match.Score.AwayGoals,
            match.State.ToString(),
            match.ElapsedTimeInSeconds,
            match.Ball.Position.X,
            match.Ball.Position.Y
        );
    }

    private Match GetMatchOrThrow(Guid matchId)
    {

        if (!_activeMatches.TryGetValue(matchId, out var match))
        {
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");
        }

        return match;
    }
}