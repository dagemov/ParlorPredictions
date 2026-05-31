namespace ParlorPrediction.Mvc.Models.Dashboard;

public sealed class PrepDashboardViewModel
{
    public DateOnly TargetDate { get; set; }

    public DateOnly WeeklyWindowEndDate { get; set; }

    public bool HasRecommendation { get; set; }

    public int RequiredBalls { get; set; }

    public int AvailableBalls { get; set; }

    public int MissingBalls { get; set; }

    public int RecommendedCases { get; set; }

    public int RecommendedLoads { get; set; }

    public int PendingTasks { get; set; }

    public int CompletedTasks { get; set; }

    public int WeeklyNeededBalls { get; set; }

    public int WeeklyCoveredBalls { get; set; }

    public int WeeklyPendingBalls { get; set; }

    public int WeeklyCompletedTasks { get; set; }

    public int WeeklyPendingTasks { get; set; }

    public int WeeklyUpcomingEventBalls { get; set; }

    public string? LastRecommendationReason { get; set; }

    public DateTime? LastRecommendationSavedAtUtc { get; set; }

    public AiRecommendationPanelViewModel AiRecommendation { get; set; } = new();
}
