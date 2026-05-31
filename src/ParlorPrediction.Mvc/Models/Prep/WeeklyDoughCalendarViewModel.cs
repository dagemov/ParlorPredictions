namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class WeeklyDoughCalendarViewModel
{
    public DateOnly SelectedDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int WeekTotalNeededBalls { get; set; }

    public int WeekCompletedBalls { get; set; }

    public int WeekMissingBalls { get; set; }

    public int UpcomingEventBalls { get; set; }

    public IReadOnlyList<WeeklyDoughCalendarDayViewModel> Days { get; set; } = Array.Empty<WeeklyDoughCalendarDayViewModel>();
}
