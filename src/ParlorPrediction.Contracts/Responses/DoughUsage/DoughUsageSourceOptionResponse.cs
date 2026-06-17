namespace ParlorPrediction.Contracts.Responses.DoughUsage;

public sealed class DoughUsageSourceOptionResponse
{
    public Guid SourceDoughBatchQualityRecordId { get; set; }

    public DateOnly UsageDate { get; set; }

    public DateOnly SourceDate { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public int AgeDays { get; set; }

    public int OriginalBalls { get; set; }

    public int UsedBalls { get; set; }

    public int RemainingBalls { get; set; }

    public string RecommendedAction { get; set; } = string.Empty;

    public bool IsPreferredSource { get; set; }

    public bool HasWarning { get; set; }

    public string? WarningMessage { get; set; }
}
