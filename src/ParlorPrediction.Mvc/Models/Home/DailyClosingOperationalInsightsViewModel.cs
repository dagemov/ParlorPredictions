namespace ParlorPrediction.Mvc.Models.Home;

public sealed class DailyClosingOperationalInsightsViewModel
{
    public int AccumulatedVariance { get; set; }

    public int AccumulatedSurplus { get; set; }

    public int AccumulatedShortage { get; set; }

    public int TotalActualUsedBalls { get; set; }

    public int ClosedDaysCount { get; set; }

    public int CurrentAvailableBalls { get; set; }

    public int StillFermentingBalls { get; set; }

    public int MixedButNotBalledBalls { get; set; }

    public int RemainingForecastNeed { get; set; }

    public int AdjustedRemainingForecastNeed { get; set; }

    public int DailyClosingVarianceApplied { get; set; }

    public int ProjectedSurplus { get; set; }

    public bool HasSurplusWarning { get; set; }

    public bool HasShortageWarning { get; set; }

    public int TotalTracedUsedBallsOnClosedDays { get; set; }

    public int TraceReconciliationDifferenceBalls { get; set; }

    public bool HasTraceReconciliationWarning { get; set; }

    public string? TraceReconciliationMessage { get; set; }

    public string Recommendation { get; set; } = string.Empty;

    public bool HasDailyClosingData => ClosedDaysCount > 0 || HasSurplusWarning || HasShortageWarning || HasTraceReconciliationWarning;
}
