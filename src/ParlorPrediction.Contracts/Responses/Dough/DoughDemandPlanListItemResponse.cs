namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class DoughDemandPlanListItemResponse
{
    public Guid Id { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public int MinDoughBalls { get; set; }

    public int MaxDoughBalls { get; set; }

    public int BaselineDoughBalls { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
