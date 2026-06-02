using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class WeeklyGoalProgressViewModel
{
    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int CurrentAvailableBalls { get; set; }

    public int DoughNeededBalls { get; set; }

    public int DoughFinishedBalls { get; set; }

    public int DoughStillMissingBalls { get; set; }

    public int DoughNeededLoads => ToLoads(DoughNeededBalls);

    public int DoughFinishedLoads => ToLoads(DoughFinishedBalls);

    public int DoughStillMissingLoads => ToLoads(DoughStillMissingBalls);

    public int CurrentAvailableLoads => ToLoads(CurrentAvailableBalls);

    private static int ToLoads(int balls)
    {
        return balls <= 0
            ? 0
            : (int)Math.Ceiling(balls / (double)DoughRules.StandardBatchBalls);
    }
}
