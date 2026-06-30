namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class DoughInventoryImpactResponse
{
    public DateOnly ReferenceDate { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int WeeklyGoalBalls { get; set; }

    public int ReadyNowBalls { get; set; }

    public int StillMissingBalls { get; set; }

    public int UseFirstBalls { get; set; }

    public int AttentionBalls { get; set; }

    public int MixedButNotBalledBalls { get; set; }

    public int FutureBalls { get; set; }

    public int UsedTodayBalls { get; set; }

    public int LostOrDiscardedBalls { get; set; }

    public int RemainingTrackedBalls { get; set; }

    public IReadOnlyList<DoughInventoryImpactSourceResponse> RemainingSources { get; set; } =
        Array.Empty<DoughInventoryImpactSourceResponse>();
}
