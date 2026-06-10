namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class DailyDoughClosingIndexViewModel
{
    public DateOnly ReferenceDate { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int TotalForecastBalls { get; set; }

    public int TotalActualUsedBalls { get; set; }

    public int AccumulatedVariance { get; set; }

    public int AccumulatedSurplus { get; set; }

    public int AccumulatedShortage { get; set; }

    public int ClosedDaysCount { get; set; }

    public int ProjectedSurplus { get; set; }

    public bool HasSurplusWarning { get; set; }

    public bool HasShortageWarning { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public IReadOnlyList<DailyDoughClosingDayCardViewModel> Days { get; set; } = Array.Empty<DailyDoughClosingDayCardViewModel>();
}
