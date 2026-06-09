namespace ParlorPrediction.Contracts.Responses.DoughQuality;

public sealed class DoughLossAnalyticsResponse
{
    public int TotalLostBalls { get; set; }

    public DoughLossAnalyticsItemResponse[] Items { get; set; } = [];
}
