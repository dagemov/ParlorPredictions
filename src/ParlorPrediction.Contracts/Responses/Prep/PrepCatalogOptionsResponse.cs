namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class PrepCatalogOptionsResponse
{
    public IReadOnlyList<PrepItemOptionResponse> PrepItems { get; init; } = Array.Empty<PrepItemOptionResponse>();

    public IReadOnlyList<PrepStationOptionResponse> PrepStations { get; init; } = Array.Empty<PrepStationOptionResponse>();
}
