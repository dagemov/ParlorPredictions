namespace ParlorPrediction.Contracts.Requests.DoughClosing;

public sealed class GetWeeklyDoughCarryoverRequest
{
    public DateOnly WeekStartDate { get; init; }
}
