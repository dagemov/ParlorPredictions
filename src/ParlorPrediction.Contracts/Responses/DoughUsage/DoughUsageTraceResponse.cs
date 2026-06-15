namespace ParlorPrediction.Contracts.Responses.DoughUsage;

public sealed class DoughUsageTraceResponse
{
    public Guid Id { get; set; }

    public DateOnly UsageDate { get; set; }

    public Guid SourceDoughBatchQualityRecordId { get; set; }

    public DateOnly SourceDate { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public int TrayCount { get; set; }

    public int BallsPerTray { get; set; }

    public int BallsUsed { get; set; }

    public string? Notes { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
