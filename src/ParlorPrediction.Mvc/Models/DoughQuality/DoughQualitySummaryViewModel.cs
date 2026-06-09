namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualitySummaryViewModel
{
    public int GoodBalls { get; set; }

    public int AttentionBalls { get; set; }

    public int ReballedBalls { get; set; }

    public int MustUseNextDayBalls { get; set; }

    public int DiscardedBalls { get; set; }

    public int TotalAvailableBalls { get; set; }

    public bool HasAnyData =>
        GoodBalls > 0 ||
        AttentionBalls > 0 ||
        ReballedBalls > 0 ||
        MustUseNextDayBalls > 0 ||
        DiscardedBalls > 0 ||
        TotalAvailableBalls > 0;
}
