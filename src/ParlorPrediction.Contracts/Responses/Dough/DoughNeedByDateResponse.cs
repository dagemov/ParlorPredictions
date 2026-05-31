namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class DoughNeedByDateResponse
{
    public DateOnly NeedDate { get; init; }

    public int RestaurantBaselineBalls { get; init; }

    public int EventBalls { get; init; }

    public int TotalRequiredBalls { get; init; }

    public DateOnly ProductionWindowStart { get; init; }

    public DateOnly ProductionWindowEnd { get; init; }

    public DateOnly RecommendedMakeDate { get; init; }

    public bool UsesShortFermentation { get; init; }
}
