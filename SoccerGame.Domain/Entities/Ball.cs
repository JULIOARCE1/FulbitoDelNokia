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

    public Ball(Position initialPosition)
    {
        Position = initialPosition;
        Velocity = Position.Zero;
    }

    public void SetPosition(Position newPosition) => Position = newPosition;

    public void ApplyImpulse(float velocityX, float velocityY) => Velocity = new Position(velocityX, velocityY);

    public void Stop() => Velocity = Position.Zero;

    public void UpdatePosition(float deltaTime)
    {
        if (deltaTime <= 0) return;

        float newX = Position.X + (Velocity.X * deltaTime);
        float newY = Position.Y + (Velocity.Y * deltaTime);
        Position = new Position(newX, newY);
    }
}