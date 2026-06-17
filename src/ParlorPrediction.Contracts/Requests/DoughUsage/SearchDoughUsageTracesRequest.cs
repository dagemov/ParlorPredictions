namespace ParlorPrediction.Contracts.Requests.DoughUsage;

public sealed class SearchDoughUsageTracesRequest
{
    public DateOnly? UsageDateFrom { get; init; }

    public DateOnly? UsageDateTo { get; init; }

    public Guid? SourceDoughBatchQualityRecordId { get; init; }

    public string? Destination { get; init; }
}
