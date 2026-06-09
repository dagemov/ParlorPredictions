using ParlorPrediction.Mvc.Models.DoughQuality;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughKitchenSheetPageViewModel
{
    public DateOnly TargetDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public DoughRecommendationViewModel? Recommendation { get; set; }

    public DoughProductionPlanningViewModel? ProductionPlanning { get; set; }

    public WeeklyGoalProgressViewModel? WeeklyGoal { get; set; }

    public DoughQualitySummaryViewModel QualitySummary { get; set; } = new();

    public IReadOnlyList<DoughKitchenAttentionItemViewModel> AttentionItems { get; set; } = Array.Empty<DoughKitchenAttentionItemViewModel>();

    public IReadOnlyList<DoughTaskViewModel> OpenTasks { get; set; } = Array.Empty<DoughTaskViewModel>();

    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

    public int NeedTodayBalls => Recommendation?.RequiredBalls ?? 0;

    public int NeedToMakeBalls => Recommendation?.MissingBalls ?? 0;

    public int GoodDoughBalls => QualitySummary.GoodBalls;

    public int AttentionDoughBalls => QualitySummary.AttentionBalls;

    public int MustUseNextDayBalls => QualitySummary.MustUseNextDayBalls;

    public bool HasWarnings => Warnings.Count > 0;

    public bool HasAttentionItems => AttentionItems.Count > 0;

    public bool HasOpenTasks => OpenTasks.Count > 0;
}
