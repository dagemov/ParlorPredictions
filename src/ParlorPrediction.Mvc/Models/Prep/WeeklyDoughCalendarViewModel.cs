using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class WeeklyDoughCalendarViewModel
{
    public DateOnly SelectedDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

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

    public int WeekTotalNeededBalls { get; set; }

    public int ReadyNowBalls { get; set; }

    public int StillFermentingBalls { get; set; }

    public int MixedButNotBalledBalls { get; set; }

    public int FinishedThisWeekBalls { get; set; }

    public int PreviousWeekFinishedBalls { get; set; }

    public int StillMissingThisWeekBalls { get; set; }

    public int UpcomingEventBalls { get; set; }

    public IReadOnlyList<WeeklyDoughCalendarDayViewModel> Days { get; set; } = Array.Empty<WeeklyDoughCalendarDayViewModel>();

    public int WeekTotalNeededLoads => ToLoads(WeekTotalNeededBalls);

    public int ReadyNowLoads => ToLoads(ReadyNowBalls);

    public int StillFermentingLoads => ToLoads(StillFermentingBalls);

    public int MixedButNotBalledLoads => ToLoads(MixedButNotBalledBalls);

    public int FinishedThisWeekLoads => ToLoads(FinishedThisWeekBalls);

    public int PreviousWeekFinishedLoads => ToLoads(PreviousWeekFinishedBalls);

    public int StillMissingThisWeekLoads => ToLoads(StillMissingThisWeekBalls);

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
