using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoccerGame.Domain.ValueObjects;

public readonly record struct Score
{
    public int HomeGoals { get; }
    public int AwayGoals { get; }

    public Score(int homeGoals, int awayGoals)
    {
        if (homeGoals < 0 || awayGoals < 0)
            throw new ArgumentOutOfRangeException(nameof(homeGoals), "El marcador no puede contener goles negativos.");

        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }

    public static Score Initial => new(0, 0);

    public Score IncrementHome() => new(HomeGoals + 1, AwayGoals);
    public Score IncrementAway() => new(HomeGoals, AwayGoals + 1);
}
