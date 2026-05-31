namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class WeeklyDoughCalendarDayViewModel
{
    public DateOnly Date { get; set; }

    public int RestaurantDoughBalls { get; set; }

    public int EventDoughBalls { get; set; }

    public int TotalNeededBalls { get; set; }

    public int AvailableBalls { get; set; }

    public int CompletedBalls { get; set; }

    public int StillMissingBalls { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsToday { get; set; }

    public string StatusCssClass =>
        Status switch
        {
            "Covered" => "status-pill--success",
            "Needs Dough" => "status-pill--warning",
            "Event Ahead" => "status-pill--info",
            _ => "status-pill--neutral"
        };
}
