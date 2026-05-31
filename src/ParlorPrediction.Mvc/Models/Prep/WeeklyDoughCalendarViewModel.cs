using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class WeeklyDoughCalendarViewModel
{
    public DateOnly SelectedDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int WeekAvailableBalls { get; set; }

    public int WeekTotalNeededBalls { get; set; }

    public int WeekCompletedBalls { get; set; }

    public int WeekMissingBalls { get; set; }

    public int UpcomingEventBalls { get; set; }

    public IReadOnlyList<WeeklyDoughCalendarDayViewModel> Days { get; set; } = Array.Empty<WeeklyDoughCalendarDayViewModel>();

    public int WeekTotalNeededLoads => ToLoads(WeekTotalNeededBalls);

    public int WeekCompletedLoads => ToLoads(WeekCompletedBalls);

    public int WeekMissingLoads => ToLoads(WeekMissingBalls);

    public int WeekAvailableLoads => ToLoads(WeekAvailableBalls);

    private static int ToLoads(int balls)
    {
        return balls <= 0
            ? 0
            : (int)Math.Ceiling(balls / (double)DoughRules.StandardBatchBalls);
    }
}
