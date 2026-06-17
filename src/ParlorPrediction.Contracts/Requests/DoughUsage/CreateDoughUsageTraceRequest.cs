namespace ParlorPrediction.Contracts.Requests.DoughUsage;

public sealed class CreateDoughUsageTraceRequest
{
    public DateOnly UsageDate { get; init; }

    public Guid SourceDoughBatchQualityRecordId { get; init; }

    public string Destination { get; init; } = string.Empty;

    public int TrayCount { get; init; }

    public string? Notes { get; init; }

    public string CreatedByUserId { get; init; } = string.Empty;
}
