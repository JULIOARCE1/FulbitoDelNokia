using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoccerGame.Domain.ValueObjects;

public readonly record struct PlayerStats
{
    public float Speed { get; }
    public float KickPower { get; }

    public PlayerStats(float speed, float kickPower)
    {
        if (speed < 0f || kickPower < 0f)
            throw new ArgumentOutOfRangeException(nameof(speed), "Las estadísticas no pueden contener valores negativos.");

        Speed = speed;
        KickPower = kickPower;
    }

    public static PlayerStats Default => new(speed: 10f, kickPower: 15f);
}