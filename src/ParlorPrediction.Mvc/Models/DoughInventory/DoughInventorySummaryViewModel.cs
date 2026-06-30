using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.DoughInventory;

public sealed class DoughInventorySummaryViewModel
{
    public DateOnly ReferenceDate { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int WeeklyGoalBalls { get; set; }

    public int ReadyNowBalls { get; set; }

    public int StillMissingBalls { get; set; }

    public int UsedTodayBalls { get; set; }

    public int UseFirstBalls { get; set; }

    public int AttentionBalls { get; set; }

    public int MixedButNotBalledBalls { get; set; }

    public int FutureBalls { get; set; }

    public int LostOrDiscardedBalls { get; set; }

    public int RemainingTrackedBalls { get; set; }

    public int WeeklyGoalLoads => ToLoads(WeeklyGoalBalls);

    public int ReadyNowLoads => ToLoads(ReadyNowBalls);

    public int StillMissingLoads => ToLoads(StillMissingBalls);

    public int UseFirstLoads => ToLoads(UseFirstBalls);

    public int MixedButNotBalledLoads => ToLoads(MixedButNotBalledBalls);

    public int FutureLoads => ToLoads(FutureBalls);

    public bool HasAnyImpactData =>
        WeeklyGoalBalls > 0 ||
        ReadyNowBalls > 0 ||
        StillMissingBalls > 0 ||
        UsedTodayBalls > 0 ||
        UseFirstBalls > 0 ||
        AttentionBalls > 0 ||
        MixedButNotBalledBalls > 0 ||
        FutureBalls > 0 ||
        LostOrDiscardedBalls > 0 ||
        RemainingTrackedBalls > 0;

    private static int ToLoads(int balls)
    {
        return balls <= 0
            ? 0
            : (int)Math.Ceiling(balls / (double)DoughRules.StandardBatchBalls);
    }
}
