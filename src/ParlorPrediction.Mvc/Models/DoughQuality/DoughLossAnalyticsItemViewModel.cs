namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughLossAnalyticsItemViewModel
{
    public DateOnly LossDate { get; set; }

    public string LossReason { get; set; } = string.Empty;

    public int QuantityLostBalls { get; set; }

    public string DisplayReason => DoughQualityDisplayText.Format(LossReason);
}
