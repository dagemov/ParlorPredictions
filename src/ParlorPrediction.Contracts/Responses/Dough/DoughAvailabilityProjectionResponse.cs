namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class DoughAvailabilityProjectionResponse
{
    public DateOnly ReferenceDate { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public DateOnly WeekEndDate { get; init; }

    public bool HasClosingCarryover { get; init; }

    public DateOnly? CarryoverSourceWeekStartDate { get; init; }

    public DateOnly? CarryoverSourceWeekEndDate { get; init; }

    public int CarryoverReadyBalls { get; init; }

    public int CarryoverAttentionBalls { get; init; }

    public int CarryoverAvailableBalls { get; init; }

    public int CarryoverMixedButNotBalledLoads { get; init; }

    public int PreviousWeekProducedBalls { get; init; }

    public int PreviousWeekUsedBalls { get; init; }

    public int PreviousWeekLostBalls { get; init; }

    public string? CarryoverClosingNotes { get; init; }

    public int ProducedThisWeekBalls { get; init; }

    public int ActualUsedBallsThisWeek { get; init; }

    public int LostBallsThisWeek { get; init; }

    public int AvailableBalls { get; init; }

    public int RegularReadyBalls { get; init; }

    public int AttentionAvailableBalls { get; init; }

    public int MustUseNextDayBalls { get; init; }
}
