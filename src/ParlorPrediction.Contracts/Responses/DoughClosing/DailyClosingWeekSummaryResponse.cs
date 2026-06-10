namespace ParlorPrediction.Contracts.Responses.DoughClosing;

public sealed class DailyClosingWeekSummaryResponse
{
    public DateOnly ReferenceDate { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public IReadOnlyList<DailyClosingWeekDayResponse> Days { get; set; } = Array.Empty<DailyClosingWeekDayResponse>();

    public int TotalForecastBalls { get; set; }

    public int TotalActualUsedBalls { get; set; }

    public int AccumulatedVariance { get; set; }

    public int AccumulatedSurplus { get; set; }

    public int AccumulatedShortage { get; set; }

    public int ClosedDaysCount { get; set; }
}
