namespace ParlorPrediction.Contracts.Requests.DoughUsage;

public sealed class DeleteDoughUsageTraceRequest
{
    public Guid DoughUsageTraceId { get; init; }

    public string DeletedByUserId { get; init; } = string.Empty;
}
