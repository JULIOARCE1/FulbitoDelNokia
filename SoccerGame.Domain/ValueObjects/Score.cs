namespace SoccerGame.Domain.ValueObjects;

public class Score
{
    public int HomeGoals { get; private set; }
    public int AwayGoals { get; private set; }

    public Score(int homeGoals = 0, int awayGoals = 0)
    {
        HomeGoals = homeGoals;
        AwayGoals = awayGoals;
    }

    public void GoalHome() => HomeGoals++;
    public void GoalAway() => AwayGoals++;
}