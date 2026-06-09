namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughLossAnalyticsDaySummaryViewModel
{
    public DateOnly LossDate { get; set; }

    public int QuantityLostBalls { get; set; }

    public string TopReason { get; set; } = string.Empty;

    public int ReasonsCount { get; set; }

    public string DisplayTopReason => DoughQualityDisplayText.Format(TopReason);
}
