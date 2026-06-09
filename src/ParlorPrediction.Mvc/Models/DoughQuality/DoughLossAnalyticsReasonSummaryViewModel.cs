namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughLossAnalyticsReasonSummaryViewModel
{
    public string LossReason { get; set; } = string.Empty;

    public int QuantityLostBalls { get; set; }

    public int SharePercent { get; set; }

    public int LossDaysCount { get; set; }

    public string DisplayReason => DoughQualityDisplayText.Format(LossReason);
}
