namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class WeeklyDoughCalendarDayResponse
{
    public DateOnly Date { get; set; }

    public int RestaurantDoughBalls { get; set; }

    public int EventDoughBalls { get; set; }

    public int TotalNeededBalls { get; set; }

    public int AvailableBalls { get; set; }

    public int CompletedBalls { get; set; }

    public int StillMissingBalls { get; set; }

    public string Status { get; set; } = string.Empty;
}
