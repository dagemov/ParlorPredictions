using ParlorPrediction.Mvc.Models.DoughQuality;
using ParlorPrediction.Mvc.Models.Prep;

namespace ParlorPrediction.Mvc.Models.Home;

public sealed class OperationalHomePageViewModel
{
    public bool IsAuthenticatedExperience { get; set; }

    public bool CanManageEvents { get; set; }

    public bool CanSeeAdminPanel { get; set; }

    public DateOnly TargetDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public WeeklyGoalProgressViewModel? WeeklyGoal { get; set; }

    public DoughProductionPlanningViewModel? ProductionPlanning { get; set; }

    public DoughQualitySummaryViewModel QualitySummary { get; set; } = new();

    public IReadOnlyList<OperationalHomeWeekDayViewModel> WeeklyForecast { get; set; } = Array.Empty<OperationalHomeWeekDayViewModel>();

    public IReadOnlyList<OperationalHomeEventViewModel> Events { get; set; } = Array.Empty<OperationalHomeEventViewModel>();
}
