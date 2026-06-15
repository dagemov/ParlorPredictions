namespace ParlorPrediction.Contracts.Requests.DoughUsage;

public sealed class GetAvailableDoughSourcesRequest
{
    public DateOnly UsageDate { get; init; }

    public string Destination { get; init; } = string.Empty;
}
