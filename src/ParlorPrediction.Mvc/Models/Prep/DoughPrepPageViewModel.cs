using ParlorPrediction.Mvc.Models.DoughQuality;
using ParlorPrediction.Mvc.Models.Home;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughPrepPageViewModel
{
    public DateOnly TargetDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public bool CanManageRecommendations { get; set; }

    public DoughRecommendationViewModel? Recommendation { get; set; }

    public DoughProductionPlanningViewModel? ProductionPlanning { get; set; }

    public WeeklyGoalProgressViewModel? WeeklyGoal { get; set; }

    public DoughQualitySummaryViewModel QualitySummary { get; set; } = new();

    public IReadOnlyList<DoughKitchenAttentionItemViewModel> AttentionItems { get; set; } = Array.Empty<DoughKitchenAttentionItemViewModel>();

    public IReadOnlyList<DoughQualityReviewCandidateViewModel> OlderDoughCandidates { get; set; } = Array.Empty<DoughQualityReviewCandidateViewModel>();

    public IReadOnlyList<DoughTaskViewModel> Tasks { get; set; } = Array.Empty<DoughTaskViewModel>();

    public DailyClosingOperationalInsightsViewModel? DailyClosingInsights { get; set; }
}
