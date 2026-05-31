namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class WeeklyDoughCalendarResponse
{
    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int WeekTotalNeededBalls { get; set; }

    public int WeekCompletedBalls { get; set; }

    public int WeekMissingBalls { get; set; }

    public int UpcomingEventBalls { get; set; }

    public IReadOnlyList<WeeklyDoughCalendarDayResponse> Days { get; set; } = Array.Empty<WeeklyDoughCalendarDayResponse>();
}
