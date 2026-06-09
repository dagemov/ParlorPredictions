namespace ParlorPrediction.Contracts.Responses.DoughQuality;

public sealed class DoughLossAnalyticsItemResponse
{
    public DateOnly LossDate { get; set; }

    public string LossReason { get; set; } = string.Empty;

    public int QuantityLostBalls { get; set; }
}
