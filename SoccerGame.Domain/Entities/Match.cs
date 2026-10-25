using System.Numerics;
using SoccerGame.Domain.ValueObjects;

namespace SoccerGame.Domain.Entities;

public enum MatchState
{
    NotStarted,
    InPlay,
    GoalScored,
    Finished
}

public class Match
{
    public Guid Id { get; }
    public Team HomeTeam { get; }
    public Team AwayTeam { get; }
    public Ball Ball { get; }
    public Score Score { get; } = new();
    public MatchState State { get; private set; } = MatchState.NotStarted;
    public float ElapsedTimeInSeconds { get; private set; }
    public float DurationInSeconds { get; }

    public float PitchWidth { get; } = 640f;
    public float PitchHeight { get; } = 420f;

    public Match(Guid id, Team homeTeam, Team awayTeam, Ball ball, float durationInSeconds = 180f)
    {
        Id = id;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        Ball = ball;
        DurationInSeconds = durationInSeconds;
    }

    public void Start() => State = MatchState.InPlay;

    public void ResumePlay() => State = MatchState.InPlay;

    public void ScoreGoalForHomeTeam()
    {
        Score.GoalHome();
        State = MatchState.GoalScored;
        Ball.Reset();
    }

    public void ScoreGoalForAwayTeam()
    {
        Score.GoalAway();
        State = MatchState.GoalScored;
        Ball.Reset();
    }

    public void UpdateTick(float deltaTime)
    {
        if (State != MatchState.InPlay) return;

        ElapsedTimeInSeconds += deltaTime;
        if (ElapsedTimeInSeconds >= DurationInSeconds)
        {
            State = MatchState.Finished;
            return;
        }

        Ball.Update(deltaTime);
        CheckGoalScored();
    }

    private void CheckGoalScored()
    {
        float goalTop = 60f;
        float goalBottom = -60f;

        // Gol de Visitante (Arco Local X <= -320)
        if (Ball.Position.X <= -320f && Ball.Position.Y >= goalBottom && Ball.Position.Y <= goalTop)
        {
            ScoreGoalForAwayTeam();
        }
        // Gol de Local (Arco Visitante X >= 320)
        else if (Ball.Position.X >= 320f && Ball.Position.Y >= goalBottom && Ball.Position.Y <= goalTop)
        {
            ScoreGoalForHomeTeam();
        }
    }
}