namespace ParlorPrediction.Contracts.Requests.DoughQuality;

public sealed class MarkDoughAsAttentionRequest
{
    public Guid DoughBatchQualityRecordId { get; init; }

    public string StatusReason { get; init; } = string.Empty;

    public DateTime? AttentionMarkedAtUtc { get; init; }

    public string? ManagerNote { get; init; }

    public string UpdatedByUserId { get; init; } = string.Empty;
}
