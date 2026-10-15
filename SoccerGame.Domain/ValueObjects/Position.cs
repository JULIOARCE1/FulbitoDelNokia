using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoccerGame.Domain.ValueObjects
{
    public readonly record struct Position(float X, float Y)
    {
        public static Position Zero => new(0f, 0f);

        public Position MoveBy(float deltaX, float deltaY) => new(X + deltaX, Y + deltaY);

        public float DistanceTo(Position other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}
