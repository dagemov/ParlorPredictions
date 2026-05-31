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
            "In Progress" => "status-pill--info",
            "Needs Dough" => "status-pill--warning",
            _ => "status-pill--neutral"
        };
}
