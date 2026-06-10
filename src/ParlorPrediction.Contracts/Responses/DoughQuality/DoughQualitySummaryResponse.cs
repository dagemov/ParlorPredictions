namespace ParlorPrediction.Contracts.Responses.DoughQuality;

public sealed class DoughQualitySummaryResponse
{
    public int GoodBalls { get; set; }

    public int AttentionBalls { get; set; }

    public int ReballedBalls { get; set; }

    public int MustUseNextDayBalls { get; set; }

    public int DiscardedBalls { get; set; }

    public int TotalAvailableBalls { get; set; }
}
