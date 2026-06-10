using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughLossAnalyticsPageViewModel
{
    public DoughLossAnalyticsFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<SelectListItem> LossReasonOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<DoughLossAnalyticsItemViewModel> Items { get; set; } = Array.Empty<DoughLossAnalyticsItemViewModel>();

    public IReadOnlyList<DoughLossAnalyticsReasonSummaryViewModel> ReasonBreakdown { get; set; } = Array.Empty<DoughLossAnalyticsReasonSummaryViewModel>();

    public IReadOnlyList<DoughLossAnalyticsDaySummaryViewModel> DayBreakdown { get; set; } = Array.Empty<DoughLossAnalyticsDaySummaryViewModel>();

    public int TotalLostBalls { get; set; }

    public int TotalLossDays { get; set; }

    public int AverageLostBallsPerLossDay { get; set; }

    public string MostCommonReasonLabel { get; set; } = "No losses yet";

    public int MostCommonReasonBalls { get; set; }

    public string HighestLossDayLabel { get; set; } = "No loss day yet";

    public int HighestLossDayBalls { get; set; }

    public string RangeSummary { get; set; } = string.Empty;

    public string PrimaryInsight { get; set; } = string.Empty;

    public string SecondaryInsight { get; set; } = string.Empty;

    public string FutureFacingNote { get; set; } = "These loss patterns can support future recommendations.";

    public bool HasData => TotalLostBalls > 0 && Items.Count > 0;
}
