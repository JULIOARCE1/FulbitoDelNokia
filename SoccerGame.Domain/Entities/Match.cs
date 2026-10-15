using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SoccerGame.Domain.ValueObjects;

namespace SoccerGame.Domain.Entities;

public enum MatchState
{
    NotStarted,
    InPlay,
    Paused,
    GoalScored,
    Finished
}

public class Match
{
    public Guid Id { get; }
    public Team HomeTeam { get; }
    public Team AwayTeam { get; }
    public Ball Ball { get; }
    public Score Score { get; private set; }
    public MatchState State { get; private set; }
    public float ElapsedTimeInSeconds { get; private set; }
    public float DurationInSeconds { get; }

    public Match(Guid id, Team homeTeam, Team awayTeam, Ball ball, float durationInSeconds = 180f)
    {
        ArgumentNullException.ThrowIfNull(homeTeam);
        ArgumentNullException.ThrowIfNull(awayTeam);
        ArgumentNullException.ThrowIfNull(ball);

        if (durationInSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationInSeconds), "La duración debe ser positiva.");

        Id = id;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        Ball = ball;
        Score = Score.Initial;
        State = MatchState.NotStarted;
        ElapsedTimeInSeconds = 0f;
        DurationInSeconds = durationInSeconds;
    }

    public void Start()
    {
        if (State != MatchState.NotStarted)
            throw new InvalidOperationException("Solo se puede iniciar un partido que no haya comenzado.");

        State = MatchState.InPlay;
    }

    public void UpdateTick(float deltaTime)
    {
        if (State != MatchState.InPlay || deltaTime <= 0f) return;

        ElapsedTimeInSeconds += deltaTime;
        Ball.UpdatePosition(deltaTime);

        if (ElapsedTimeInSeconds >= DurationInSeconds)
        {
            ElapsedTimeInSeconds = DurationInSeconds;
            State = MatchState.Finished;
        }
    }

    public void ScoreGoalForHomeTeam()
    {
        if (State != MatchState.InPlay)
            throw new InvalidOperationException("Solo se pueden anotar goles con el partido en juego.");

        Score = Score.IncrementHome();
        State = MatchState.GoalScored;
    }

    public void ScoreGoalForAwayTeam()
    {
        if (State != MatchState.InPlay)
            throw new InvalidOperationException("Solo se pueden anotar goles con el partido en juego.");

        Score = Score.IncrementAway();
        State = MatchState.GoalScored;
    }

    public void ResumePlay()
    {
        if (State == MatchState.GoalScored || State == MatchState.Paused)
        {
            State = MatchState.InPlay;
        }
    }
}