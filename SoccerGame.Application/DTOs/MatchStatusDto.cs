using System;
using System.Collections.Generic;
using System.Text;

namespace SoccerGame.Application.DTOs;

public record MatchStatusDto(
    Guid MatchId,
    string HomeTeamName,
    string AwayTeamName,
    int HomeGoals,
    int AwayGoals,
    string State,
    float ElapsedTimeInSeconds,
    float BallX,
    float BallY
);