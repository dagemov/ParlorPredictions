namespace ParlorPrediction.Contracts.Requests.Dough;

public sealed class SaveDoughDemandPlanRequest
{
    public DayOfWeek DayOfWeek { get; init; }

    public string SourceName { get; init; } = string.Empty;

    public int MinDoughBalls { get; init; }

    public int MaxDoughBalls { get; init; }

    public string? Notes { get; init; }

    public bool IsActive { get; init; } = true;
}
