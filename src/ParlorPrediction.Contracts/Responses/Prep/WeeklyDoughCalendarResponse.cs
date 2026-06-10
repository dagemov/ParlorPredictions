namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class WeeklyDoughCalendarResponse
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

    public int WeekTotalNeededBalls { get; set; }

    public int ReadyNowBalls { get; set; }

    public int StillFermentingBalls { get; set; }

    public int MixedButNotBalledBalls { get; set; }

    public int MixedButNotBalledLoads { get; set; }

    public int FutureBalls { get; set; }

    public int FinishedThisWeekBalls { get; set; }

    public int ProducedThisWeekBalls { get; set; }

    public int PreviousWeekFinishedBalls { get; set; }

    public int StillMissingThisWeekBalls { get; set; }

    public int ActualUsedBallsThisWeek { get; set; }

    public int AccumulatedDailyVariance { get; set; }

    public int UpcomingEventBalls { get; set; }

    public IReadOnlyList<WeeklyDoughCalendarDayResponse> Days { get; set; } = Array.Empty<WeeklyDoughCalendarDayResponse>();
}
