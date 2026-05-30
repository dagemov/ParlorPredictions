namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class PrepDashboardSummaryResponse
{
    public DateOnly TargetDate { get; set; }

    public bool HasRecommendation { get; set; }

    public int RequiredBalls { get; set; }

    public int AvailableBalls { get; set; }

    public int MissingBalls { get; set; }

    public int RecommendedCases { get; set; }

    public int RecommendedLoads { get; set; }

    public int PendingTasks { get; set; }

    public int CompletedTasks { get; set; }

    public string? LastRecommendationReason { get; set; }

    public DateTime? LastRecommendationSavedAtUtc { get; set; }
}
