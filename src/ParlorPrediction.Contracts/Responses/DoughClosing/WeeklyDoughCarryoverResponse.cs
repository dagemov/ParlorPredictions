namespace ParlorPrediction.Contracts.Responses.DoughClosing;

public sealed class WeeklyDoughCarryoverResponse
{
    public DateOnly TargetWeekStartDate { get; set; }

    public DateOnly TargetWeekEndDate { get; set; }

    public bool HasClosingCarryover { get; set; }

    public DateOnly? SourceWeekStartDate { get; set; }

    public DateOnly? SourceWeekEndDate { get; set; }

    public int CarryoverReadyBalls { get; set; }

    public int CarryoverAttentionBalls { get; set; }

    public int CarryoverAvailableBalls { get; set; }

    public int MixedButNotBalledLoads { get; set; }

    public int PreviousWeekProducedBalls { get; set; }

    public int PreviousWeekUsedBalls { get; set; }

    public int PreviousWeekLostBalls { get; set; }

    public string? ClosingNotes { get; set; }
}
