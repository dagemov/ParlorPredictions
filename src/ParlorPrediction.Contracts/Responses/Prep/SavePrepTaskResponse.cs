namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class SavePrepTaskResponse
{
    public Guid PrepTaskId { get; init; }

    public DateOnly TaskDate { get; init; }

    public string PrepItemName { get; init; } = string.Empty;

    public string PrepStationName { get; init; } = string.Empty;

    public string AssignedRole { get; init; } = string.Empty;

    public int QuantityRecommended { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
