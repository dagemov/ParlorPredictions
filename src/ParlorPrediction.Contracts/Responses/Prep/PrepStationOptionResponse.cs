namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class PrepStationOptionResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
