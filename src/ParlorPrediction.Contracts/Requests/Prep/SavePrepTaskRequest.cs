namespace ParlorPrediction.Contracts.Requests.Prep;

public sealed class SavePrepTaskRequest
{
    public DateOnly TaskDate { get; init; }

    public Guid PrepItemId { get; init; }

    public Guid PrepStationId { get; init; }

    public string AssignedRole { get; init; } = string.Empty;

    public string TaskType { get; init; } = string.Empty;

    public string QuantityUnit { get; init; } = string.Empty;

    public int QuantityValue { get; init; }

    public string? Notes { get; init; }
}
