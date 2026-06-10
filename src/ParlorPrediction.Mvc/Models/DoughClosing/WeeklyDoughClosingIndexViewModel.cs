namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class WeeklyDoughClosingIndexViewModel
{
    public DateOnly ReferenceDate { get; set; }

    public DateOnly? FromWeekStartDate { get; set; }

    public DateOnly? ToWeekStartDate { get; set; }

    public WeeklyDoughCarryoverPreviewViewModel CarryoverPreview { get; set; } = new();

    public WeeklyDailyClosingSummaryViewModel DailyClosingSummary { get; set; } = new();

    public IReadOnlyList<WeeklyDoughClosingListItemViewModel> Closings { get; set; } = Array.Empty<WeeklyDoughClosingListItemViewModel>();
}
