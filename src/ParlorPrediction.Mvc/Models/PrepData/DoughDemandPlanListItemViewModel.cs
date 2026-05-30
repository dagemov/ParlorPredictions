namespace ParlorPrediction.Mvc.Models.PrepData;

public sealed class DoughDemandPlanListItemViewModel
{
    public Guid Id { get; init; }

    public DayOfWeek DayOfWeek { get; init; }

    public string SourceName { get; init; } = string.Empty;

    public int MinDoughBalls { get; init; }

    public int MaxDoughBalls { get; init; }

    public int BaselineDoughBalls { get; init; }

    public string? Notes { get; init; }

    public bool IsActive { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
