namespace ParlorPrediction.Contracts.Responses.DoughQuality;

public sealed class DoughBatchQualityRecordResponse
{
    public Guid Id { get; set; }

    public DateOnly SourceDate { get; set; }

    public Guid? OriginalDoughTaskId { get; set; }

    public DateTime CreatedOrBalledAt { get; set; }

    public int QuantityBalls { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;

    public string? StatusReason { get; set; }

    public DateTime? AttentionMarkedAt { get; set; }

    public DateTime? ReballedAt { get; set; }

    public DateOnly? MustUseByDate { get; set; }

    public DateTime? DiscardedAt { get; set; }

    public string? DiscardReason { get; set; }

    public string? ManagerNote { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool CountsAsAvailable { get; set; }
}
