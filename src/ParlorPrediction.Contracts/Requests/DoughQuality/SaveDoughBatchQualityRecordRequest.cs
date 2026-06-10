namespace ParlorPrediction.Contracts.Requests.DoughQuality;

public sealed class SaveDoughBatchQualityRecordRequest
{
    public DateOnly SourceDate { get; init; }

    public Guid? OriginalDoughTaskId { get; init; }

    public DateTime CreatedOrBalledAt { get; init; }

    public int QuantityBalls { get; init; }

    public string InitialStatus { get; init; } = "Good";

    public string? StatusReason { get; init; }

    public DateOnly? MustUseByDate { get; init; }

    public string? DiscardReason { get; init; }

    public string? ManagerNote { get; init; }

    public string CreatedByUserId { get; init; } = string.Empty;
}
