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

    // Fricción del césped (coeficiente de desaceleración por segundo)
    private const float Friction = 2.5f;

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

        // 1. Aplicar movimiento según la velocidad actual
        float newX = Position.X + (Velocity.X * deltaTime);
        float newY = Position.Y + (Velocity.Y * deltaTime);
        Position = new Position(newX, newY);

        // 2. Aplicar rozamiento/fricción para desacelerar la pelota
        float currentSpeed = MathF.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);

        if (currentSpeed > 0f)
        {
            float newSpeed = MathF.Max(0f, currentSpeed - (Friction * currentSpeed * deltaTime));

            if (newSpeed <= 0.05f)
            {
                Stop();
            }
            else
            {
                float factor = newSpeed / currentSpeed;
                Velocity = new Position(Velocity.X * factor, Velocity.Y * factor);
            }
        }
    }
}