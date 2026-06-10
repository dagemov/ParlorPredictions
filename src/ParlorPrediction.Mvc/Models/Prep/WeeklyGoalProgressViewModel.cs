using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class WeeklyGoalProgressViewModel
{
    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public bool HasClosingCarryover { get; set; }

    public DateOnly? CarryoverSourceWeekStartDate { get; set; }

    public DateOnly? CarryoverSourceWeekEndDate { get; set; }

    public int CarryoverReadyBalls { get; set; }

    public int CarryoverAttentionBalls { get; set; }

    public int CarryoverAvailableBalls { get; set; }

    public int CarryoverMixedButNotBalledLoads { get; set; }

    public int CarryoverMixedButNotBalledPotentialBalls { get; set; }

    public int PreviousWeekProducedBalls { get; set; }

    public int PreviousWeekLostBalls { get; set; }

    public string? CarryoverClosingNotes { get; set; }

    public int DoughNeededBalls { get; set; }

    public int ReadyNowBalls { get; set; }

    public int StillFermentingBalls { get; set; }

    public int MixedButNotBalledBalls { get; set; }

    public int MixedButNotBalledLoadCount { get; set; }

    public int FutureBalls { get; set; }

    public int FinishedThisWeekBalls { get; set; }

    public int ProducedThisWeekBalls { get; set; }

    public int PreviousWeekFinishedBalls { get; set; }

    public int DoughStillMissingThisWeekBalls { get; set; }

    public int ActualUsedBallsThisWeek { get; set; }

    public int AccumulatedDailyVariance { get; set; }

    public int DoughNeededLoads => ToLoads(DoughNeededBalls);

    public int ReadyNowLoads => ToLoads(ReadyNowBalls);

    public int StillFermentingLoads => ToLoads(StillFermentingBalls);

    public int MixedButNotBalledLoads => MixedButNotBalledLoadCount > 0
        ? MixedButNotBalledLoadCount
        : ToLoads(MixedButNotBalledBalls);

    public int FutureBallsLoads => ToLoads(FutureBalls);

    public int ProducedThisWeekLoads => ToLoads(ProducedThisWeekBalls);

    public int FinishedThisWeekLoads => ToLoads(FinishedThisWeekBalls);

    public int PreviousWeekFinishedLoads => ToLoads(PreviousWeekFinishedBalls);

    public int DoughStillMissingThisWeekLoads => ToLoads(DoughStillMissingThisWeekBalls);

    public int CarryoverAvailableLoads => ToLoads(CarryoverAvailableBalls);

    public int CarryoverMixedButNotBalledPotentialLoads => ToLoads(CarryoverMixedButNotBalledPotentialBalls);

    public int PreviousWeekProducedLoads => ToLoads(PreviousWeekProducedBalls);

    public int PreviousWeekLostLoads => ToLoads(PreviousWeekLostBalls);

    private static int ToLoads(int balls)
    {
        return balls <= 0
            ? 0
            : (int)Math.Ceiling(balls / (double)DoughRules.StandardBatchBalls);
    }
}
