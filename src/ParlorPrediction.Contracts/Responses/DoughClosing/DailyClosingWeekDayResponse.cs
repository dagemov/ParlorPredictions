namespace ParlorPrediction.Contracts.Responses.DoughClosing;

public sealed class DailyClosingWeekDayResponse
{
    public DateOnly Date { get; set; }

    public int ForecastNeededBalls { get; set; }

    public int? ActualUsedBalls { get; set; }

    public int? DailyVariance { get; set; }

    public bool IsClosed { get; set; }

    public Guid? DailyClosingId { get; set; }

    public string? Notes { get; set; }

    public bool IsToday { get; set; }

    public bool IsFuture { get; set; }
}
