namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class PrepItemOptionResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public Guid PrepStationId { get; init; }

    public string PrepStationName { get; init; } = string.Empty;
}
