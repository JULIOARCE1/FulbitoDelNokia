using System;
using System.Collections.Generic;
using System.Text;

namespace SoccerGame.Application.Commands;

public record StartMatchCommand(Guid MatchId);

public record UpdateTickCommand(Guid MatchId, float DeltaTime)
{
    // Sanitización de input para DeltaTime
    public float SanitizedDeltaTime => float.IsNaN(DeltaTime) || float.IsInfinity(DeltaTime) || DeltaTime < 0f
        ? 0f
        : Math.Min(DeltaTime, 0.1f); // Límite máximo de 100ms por tick para evitar saltos bruscos
}

public record MovePlayerCommand(Guid MatchId, Guid PlayerId, float DeltaX, float DeltaY)
{
    // Sanitización de deltas de movimiento
    public float SanitizedDeltaX => float.IsNaN(DeltaX) || float.IsInfinity(DeltaX) ? 0f : Math.Clamp(DeltaX, -1f, 1f);
    public float SanitizedDeltaY => float.IsNaN(DeltaY) || float.IsInfinity(DeltaY) ? 0f : Math.Clamp(DeltaY, -1f, 1f);
}

public record KickBallCommand(Guid MatchId, Guid? PlayerId, float DirectionX, float DirectionY, float Power);