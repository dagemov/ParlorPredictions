namespace ParlorPrediction.Contracts.Requests.DoughQuality;

public sealed class ReballDoughRequest
{
    public Guid DoughBatchQualityRecordId { get; init; }

    public int QuantityRecoveredBalls { get; init; }

    public DateTime ReballDateUtc { get; init; }

    public string Result { get; init; } = "PartialRecovered";

    public string? DiscardReason { get; init; }

    public string? ManagerNote { get; init; }

    public string UpdatedByUserId { get; init; } = string.Empty;
}
