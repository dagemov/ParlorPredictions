namespace ParlorPrediction.Contracts.Responses.DoughClosing;

public sealed class DailyDoughClosingResponse
{
    public Guid Id { get; set; }

    public DateOnly ClosingDate { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public int ForecastNeededBalls { get; set; }

    public int ActualUsedBalls { get; set; }

    public int DailyVariance { get; set; }

    public string? Notes { get; set; }

    public string ClosedByUserId { get; set; } = null!;

    public DateTime ClosedAtUtc { get; set; }

    public bool WasCorrected { get; set; }

    public string? CorrectedByUserId { get; set; }

    public DateTime? CorrectedAtUtc { get; set; }

    public string? CorrectionNote { get; set; }
}
