namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class WeeklyDailyClosingSummaryViewModel
{
    public DateOnly ServiceStartDate { get; set; }

    public DateOnly ServiceEndDate { get; set; }

    public int TotalForecastBalls { get; set; }

    public int TotalActualUsedBalls { get; set; }

    public int AccumulatedVariance { get; set; }

    public int AccumulatedSurplus { get; set; }

    public int AccumulatedShortage { get; set; }

    public int ClosedDaysCount { get; set; }

    public bool HasDailyClosings => ClosedDaysCount > 0;
}
