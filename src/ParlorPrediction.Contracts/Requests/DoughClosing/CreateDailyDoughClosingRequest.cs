namespace ParlorPrediction.Contracts.Requests.DoughClosing;

public sealed class CreateDailyDoughClosingRequest
{
    public DateOnly ClosingDate { get; set; }

    public int ForecastNeededBalls { get; set; }

    public int ActualUsedBalls { get; set; }

    public string? Notes { get; set; }

    public string ClosedByUserId { get; set; } = null!;

    public DateTime? ClosedAtUtc { get; set; }
}
