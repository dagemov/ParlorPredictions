namespace ParlorPrediction.Mvc.Models.Home;

public sealed class OperationalHomeWeekDayViewModel
{
    public DateOnly Date { get; set; }

    public int TotalNeededBalls { get; set; }

    public int EventBalls { get; set; }

    public int AvailableBalls { get; set; }

    public int CompletedBalls { get; set; }

    public int StillMissingBalls { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsToday { get; set; }
}
