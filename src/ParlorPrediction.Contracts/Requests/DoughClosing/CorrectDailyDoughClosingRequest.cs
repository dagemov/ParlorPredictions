namespace ParlorPrediction.Contracts.Requests.DoughClosing;

public sealed class CorrectDailyDoughClosingRequest
{
    public Guid DailyDoughClosingId { get; set; }

    public int ForecastNeededBalls { get; set; }

    public int ActualUsedBalls { get; set; }

    public string? Notes { get; set; }

    public string? CorrectionNote { get; set; }

    public string CorrectedByUserId { get; set; } = null!;

    public DateTime? CorrectedAtUtc { get; set; }
}
