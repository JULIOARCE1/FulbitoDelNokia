using System;
using System.Collections.Generic;
using System.Text;

using SoccerGame.Application.Commands;
using SoccerGame.Application.Services;
using Xunit;

namespace SoccerGame.Application.Tests;

public class MatchApplicationServiceTests
{
    [Fact]
    public void Service_ShouldCreateAndStartMatchSuccessfully()
    {
        var service = new MatchApplicationService();
        var matchId = service.CreateMatch("River Plate", "Boca Juniors");

        service.StartMatch(new StartMatchCommand(matchId));
        var status = service.GetMatchStatus(matchId);

        Assert.NotNull(status);
        Assert.Equal("InPlay", status.State);
    }

    [Fact]
    public void Service_UpdateTick_WithCorruptedDeltaTime_ShouldSanitizeAndAdvanceTime()
    {
        var service = new MatchApplicationService();
        var matchId = service.CreateMatch("River Plate", "Boca Juniors");
        service.StartMatch(new StartMatchCommand(matchId));

        // Enviamos un DeltaTime invandado (NaN)
        service.UpdateTick(new UpdateTickCommand(matchId, float.NaN));
        var status = service.GetMatchStatus(matchId);

        Assert.NotNull(status);
        Assert.Equal(0f, status.ElapsedTimeInSeconds); // No debe romper la simulación
    }
}
