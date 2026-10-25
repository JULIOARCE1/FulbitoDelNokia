using System.Numerics;

namespace SoccerGame.Domain.ValueObjects;

public struct Position
{
    public float X { get; }
    public float Y { get; }

    public static Position Zero => new(0f, 0f);

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float DistanceTo(Position other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public Position MoveBy(float dx, float dy)
    {
        return new Position(X + dx, Y + dy);
    }

    // Conversiones automáticas a Vector2 para interoperabilidad con Ball
    public static implicit operator Vector2(Position pos) => new(pos.X, pos.Y);
    public static implicit operator Position(Vector2 vec) => new(vec.X, vec.Y);
}