namespace ParlorPrediction.Contracts.Requests.DoughUsage;

public sealed class CorrectDoughUsageTraceRequest
{
    public Guid DoughUsageTraceId { get; init; }

    public DateOnly UsageDate { get; init; }

    public Guid SourceDoughBatchQualityRecordId { get; init; }

    public string Destination { get; init; } = string.Empty;

    public int TrayCount { get; init; }

    public string? Notes { get; init; }

    public string UpdatedByUserId { get; init; } = string.Empty;
}
