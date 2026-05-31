namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughRecommendationViewModel
{
    public Guid? RecommendationId { get; set; }

    public DateOnly RecommendationDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public int RequiredBalls { get; set; }

    public int HistoricalAverageBalls { get; set; }

    public int EventEstimatedBalls { get; set; }

    public int AvailableBalls { get; set; }

    public int CompletedBalls { get; set; }

    public int MissingBalls { get; set; }

    public int RecommendedCases { get; set; }

    public int RecommendedLoads { get; set; }

    public bool ShouldMakeDough { get; set; }

    public bool ShouldBallDough { get; set; }

    public bool UsesShortFermentationException { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool IsPersisted { get; set; }

    public bool CanSaveRecommendation { get; set; }

    public bool CanCreateTask { get; set; }

    public bool TaskAlreadyExists { get; set; }

    public DateTime? SavedAtUtc { get; set; }

    public IReadOnlyList<string> ActionPlanSteps { get; set; } = Array.Empty<string>();
}
