using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SoccerGame.Domain.Entities;
using SoccerGame.Domain.ValueObjects;
using Xunit;

namespace SoccerGame.Domain.Tests;

public class MatchTests
{
    [Fact]
    public void Match_Start_ShouldChangeStateToInPlay()
    {
        var match = CreateSampleMatch();
        match.Start();

        Assert.Equal(MatchState.InPlay, match.State);
    }

    [Fact]
    public void Match_ScoreGoalForHomeTeam_ShouldUpdateScoreAndChangeState()
    {
        var match = CreateSampleMatch();
        match.Start();

        match.ScoreGoalForHomeTeam();

        Assert.Equal(1, match.Score.HomeGoals);
        Assert.Equal(MatchState.GoalScored, match.State);
    }

    [Fact]
    public void Match_UpdateTick_ShouldFinishMatch_WhenDurationReached()
    {
        var match = CreateSampleMatch(durationSeconds: 60f);
        match.Start();

        match.UpdateTick(60f);

        Assert.Equal(MatchState.Finished, match.State);
    }

    private static Match CreateSampleMatch(float durationSeconds = 180f)
    {
        var home = new Team(Guid.NewGuid(), "River Plate");
        var away = new Team(Guid.NewGuid(), "Boca Juniors");
        var ball = new Ball(Position.Zero);

        return new Match(Guid.NewGuid(), home, away, ball, durationSeconds);
    }
}