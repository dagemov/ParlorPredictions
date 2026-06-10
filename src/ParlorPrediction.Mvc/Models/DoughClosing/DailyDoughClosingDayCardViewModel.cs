namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class DailyDoughClosingDayCardViewModel
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

    public string VarianceLabel => DailyVariance switch
    {
        > 0 => $"+{DailyVariance} Balls",
        < 0 => $"{DailyVariance} Balls",
        0 => "0 Balls",
        null => "—"
    };
}
