namespace ParlorPrediction.Contracts.Requests.DoughUsage;

public sealed class GetDoughReballPlanningRequest
{
    public DateOnly ReferenceDate { get; init; }
}
