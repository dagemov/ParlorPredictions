namespace ParlorPrediction.Contracts.Requests.Prep;

public sealed class AdminCorrectPrepTaskRequest
{
    public Guid PrepTaskId { get; init; }

    public DateOnly TaskDate { get; init; }

    public string TaskType { get; init; } = string.Empty;

    public string QuantityUnit { get; init; } = string.Empty;

    public int QuantityRecommended { get; init; }

    public string Status { get; init; } = string.Empty;

    public int QuantityCompleted { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public string? CompletedByUserId { get; init; }

    public Guid? SourcePrepTaskId { get; init; }

    public Guid? SourceDoughBatchId { get; init; }

    public string? Notes { get; init; }

    public string UpdatedByUserId { get; init; } = string.Empty;
}
