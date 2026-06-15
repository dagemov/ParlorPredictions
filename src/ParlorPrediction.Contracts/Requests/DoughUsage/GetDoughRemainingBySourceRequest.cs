namespace ParlorPrediction.Contracts.Requests.DoughUsage;

public sealed class GetDoughRemainingBySourceRequest
{
    public DateOnly ReferenceDate { get; init; }
}
