using System;
using System.Collections.Generic;
using System.Text;

namespace SoccerGame.Application.DTOs;

public record PlayerDto(Guid Id, string Name, int ShirtNumber, float X, float Y, bool IsHomeTeam);

public record MatchStatusDto(
    Guid MatchId,
    string HomeTeamName,
    string AwayTeamName,
    int HomeGoals,
    int AwayGoals,
    string State,
    float ElapsedTimeInSeconds,
    float BallX,
    float BallY,
    IReadOnlyCollection<PlayerDto> Players
);