namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class DoughTaskListItemResponse
{
    public Guid PrepTaskId { get; set; }

    public Guid? DoughPrepRecommendationId { get; set; }

    public DateOnly TaskDate { get; set; }

    public Guid PrepItemId { get; set; }

    public string PrepItemName { get; set; } = string.Empty;

    public string PrepItemCode { get; set; } = string.Empty;

    public Guid PrepStationId { get; set; }

    public string PrepStationName { get; set; } = string.Empty;

    public string PrepStationCode { get; set; } = string.Empty;

    public string AssignedRole { get; set; } = string.Empty;

    public string TaskType { get; set; } = string.Empty;

    public string QuantityUnit { get; set; } = string.Empty;

    public int QuantityRecommended { get; set; }

    public int QuantityCompleted { get; set; }

    public int QuantityRecommendedBallsEquivalent { get; set; }

    public int QuantityCompletedBallsEquivalent { get; set; }

    public bool CountsAsAvailableBallsWhenCompleted { get; set; }

    public Guid? SourcePrepTaskId { get; set; }

    public Guid? SourceDoughBatchId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string? CompletedByUserId { get; set; }

    public string? CompletedByUserName { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsManualTask { get; set; }
}
