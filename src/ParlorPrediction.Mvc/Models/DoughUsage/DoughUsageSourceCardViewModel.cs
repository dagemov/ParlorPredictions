namespace ParlorPrediction.Mvc.Models.DoughUsage;

public sealed class DoughUsageSourceCardViewModel
{
    public Guid SourceDoughBatchQualityRecordId { get; set; }

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

    public bool IsSelected { get; set; }

    public string Title => $"Dough from {SourceDate:dddd, MMM d}";

    public string DisplaySourceType => DoughUsageDisplayText.Format(SourceType);

    public string DisplayRecommendedAction => DoughUsageDisplayText.Format(RecommendedAction);

    public int UsedPercent => OriginalBalls <= 0
        ? 0
        : Math.Clamp((int)Math.Round((double)UsedBalls * 100 / OriginalBalls), 0, 100);

    public int RemainingPercent => OriginalBalls <= 0
        ? 0
        : Math.Clamp((int)Math.Round((double)RemainingBalls * 100 / OriginalBalls), 0, 100);
}
