namespace ParlorPrediction.Contracts.Requests.Dough;

public sealed class DoughProductionPlanningRequest
{
    public DateOnly ProductionDate { get; init; }

    public int DaysAhead { get; init; } = 7;
}
