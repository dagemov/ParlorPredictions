namespace ParlorPrediction.Contracts.Requests.Prep;

public sealed class SavePrepTaskRequest
{
    public DateOnly TaskDate { get; init; }

    public Guid PrepItemId { get; init; }

    public Guid PrepStationId { get; init; }

    public string AssignedRole { get; init; } = string.Empty;

    public int QuantityRecommended { get; init; }

    public string? Notes { get; init; }
}
