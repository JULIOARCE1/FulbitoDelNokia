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

    // Definición conceptual del campo (ancho: 700, alto: 500)
    public float PitchWidth { get; } = 700f;
    public float PitchHeight { get; } = 500f;

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

        CheckGoalConditions();

        if (ElapsedTimeInSeconds >= DurationInSeconds)
        {
            ElapsedTimeInSeconds = DurationInSeconds;
            State = MatchState.Finished;
        }
    }

    private void CheckGoalConditions()
    {
        float halfWidth = PitchWidth / 2f;
        float halfGoalHeight = 60f; // Alto del arco

        // Gol del equipo visitante (Arco izquierdo)
        if (Ball.Position.X < -halfWidth && MathF.Abs(Ball.Position.Y) < halfGoalHeight)
        {
            ScoreGoalForAwayTeam();
            ResetPositionsAfterGoal();
        }
        // Gol del equipo local (Arco derecho)
        else if (Ball.Position.X > halfWidth && MathF.Abs(Ball.Position.Y) < halfGoalHeight)
        {
            ScoreGoalForHomeTeam();
            ResetPositionsAfterGoal();
        }
    }

    private void ResetPositionsAfterGoal()
    {
        Ball.SetPosition(Position.Zero);
        Ball.Stop();
    }

    public void ScoreGoalForHomeTeam()
    {
        if (State != MatchState.InPlay) return;

        Score = Score.IncrementHome();
        State = MatchState.GoalScored;
    }

    public void ScoreGoalForAwayTeam()
    {
        if (State != MatchState.InPlay) return;

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