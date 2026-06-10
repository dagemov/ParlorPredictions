namespace ParlorPrediction.Contracts.Requests.DoughClosing;

public sealed class GetDailyClosingWeekSummaryRequest
{
    public DateOnly ReferenceDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;
}
