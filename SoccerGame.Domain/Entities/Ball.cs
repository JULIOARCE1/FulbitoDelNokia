using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SoccerGame.Domain.ValueObjects;

namespace SoccerGame.Domain.Entities;

public class Ball
{
    public Position Position { get; private set; }
    public Position Velocity { get; private set; }
    public Guid? PossessorId { get; private set; }

    private const float Friction = 3.0f;

    public Ball(Position initialPosition)
    {
        Position = initialPosition;
        Velocity = Position.Zero;
        PossessorId = null;
    }

    public void SetPosition(Position newPosition) => Position = newPosition;

    public void Stop()
    {
        Velocity = Position.Zero;
        PossessorId = null;
    }

    public void GiveTo(Guid playerId, Position position)
    {
        PossessorId = playerId;
        Position = position;
        Velocity = Position.Zero;
    }

    public void ApplyImpulse(float velocityX, float velocityY)
    {
        PossessorId = null;
        Velocity = new Position(velocityX, velocityY);
    }

    public void UpdatePosition(float deltaTime)
    {
        if (deltaTime <= 0f || PossessorId.HasValue) return;

        float newX = Position.X + (Velocity.X * deltaTime);
        float newY = Position.Y + (Velocity.Y * deltaTime);
        Position = new Position(newX, newY);

        float currentSpeed = MathF.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);

        if (currentSpeed > 0f)
        {
            float newSpeed = MathF.Max(0f, currentSpeed - (Friction * currentSpeed * deltaTime));
            if (newSpeed <= 0.1f)
            {
                Velocity = Position.Zero;
            }
            else
            {
                float factor = newSpeed / currentSpeed;
                Velocity = new Position(Velocity.X * factor, Velocity.Y * factor);
            }
        }
    }
}