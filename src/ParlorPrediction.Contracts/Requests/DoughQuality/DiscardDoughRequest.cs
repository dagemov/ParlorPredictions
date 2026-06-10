namespace ParlorPrediction.Contracts.Requests.DoughQuality;

public sealed class DiscardDoughRequest
{
    public Guid DoughBatchQualityRecordId { get; init; }

    public string DiscardReason { get; init; } = string.Empty;

    public DateTime? DiscardedAtUtc { get; init; }

    public string? ManagerNote { get; init; }

    public string UpdatedByUserId { get; init; } = string.Empty;
}
