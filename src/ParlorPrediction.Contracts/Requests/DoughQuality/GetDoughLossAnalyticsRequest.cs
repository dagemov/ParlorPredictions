namespace ParlorPrediction.Contracts.Requests.DoughQuality;

public sealed class GetDoughLossAnalyticsRequest
{
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public string? LossReason { get; init; }
}
