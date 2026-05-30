namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class DoughPrepCalculationResult
{
    public DateOnly TargetDate { get; set; }

    public int RequiredBalls { get; set; }

    public int HistoricalAverageBalls { get; set; }

    public int EventEstimatedBalls { get; set; }

    public int AvailableBalls { get; set; }

    public int MissingBalls { get; set; }

    public int RecommendedCases { get; set; }

    public int RecommendedLoads { get; set; }

    public bool ShouldMakeDough { get; set; }

    public bool ShouldBallDough { get; set; }

    public bool UsesShortFermentationException { get; set; }

    public string Reason { get; set; } = string.Empty;
}
