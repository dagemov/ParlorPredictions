namespace ParlorPrediction.Contracts.Requests.Dough;

public sealed class GetDoughInventoryImpactRequest
{
    public DateOnly ReferenceDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;
}
