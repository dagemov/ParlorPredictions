namespace ParlorPrediction.Contracts.Requests.DoughQuality;

public sealed class CorrectDoughQualityStatusRequest
{
    public Guid DoughBatchQualityRecordId { get; init; }

    public string NewStatus { get; init; } = string.Empty;

    public string? StatusReason { get; init; }

    public DateTime? EffectiveAtUtc { get; init; }

    public DateOnly? MustUseByDate { get; init; }

    public string? DiscardReason { get; init; }

    public string? ManagerNote { get; init; }

    public string UpdatedByUserId { get; init; } = string.Empty;
}
