namespace ParlorPrediction.Contracts.Requests.DoughQuality;

public sealed class SearchDoughBatchQualityRecordsRequest
{
    public DateOnly? SourceDateFrom { get; init; }

    public DateOnly? SourceDateTo { get; init; }

    public DateOnly? CreatedOrBalledFromDate { get; init; }

    public DateOnly? CreatedOrBalledToDate { get; init; }

    public DateOnly? ReballedFromDate { get; init; }

    public DateOnly? ReballedToDate { get; init; }

    public string? CurrentStatus { get; init; }
}
