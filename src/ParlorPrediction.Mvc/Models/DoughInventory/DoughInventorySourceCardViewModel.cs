using ParlorPrediction.Mvc.Models.DoughUsage;

namespace ParlorPrediction.Mvc.Models.DoughInventory;

public sealed class DoughInventorySourceCardViewModel
{
    public Guid SourceDoughBatchQualityRecordId { get; set; }

    public DateOnly SourceDate { get; set; }

    public DateTime CreatedOrBalledAt { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public DateOnly? MustUseByDate { get; set; }

    public int AgeDays { get; set; }

    public int OriginalBalls { get; set; }

    public int UsedBalls { get; set; }

    public int RemainingBalls { get; set; }

    public bool CountsAsAvailable { get; set; }

    public bool IsReballCandidate { get; set; }

    public bool IsDiscardCandidate { get; set; }

    public string RecommendedAction { get; set; } = string.Empty;

    public string Title => $"Dough from {SourceDate:dddd, MMM d}";

    public string DisplaySourceType => DoughUsageDisplayText.Format(SourceType);

    public string DisplayRecommendedAction => DoughUsageDisplayText.Format(RecommendedAction);

    public int RemainingPercent => OriginalBalls <= 0
        ? 0
        : Math.Clamp((int)Math.Round((double)RemainingBalls * 100 / OriginalBalls), 0, 100);

    public string RecommendationBadgeClass => RecommendedAction switch
    {
        "UseFirst" => "bg-secondary-fixed text-on-secondary-fixed-variant",
        "Review" => "bg-secondary-container/20 text-on-secondary-container",
        "Reball" => "bg-primary-fixed text-on-primary-fixed-variant",
        "Discard" => "bg-error-container text-on-error-container",
        _ => "bg-surface-container text-on-surface-variant"
    };

    public string StatusBadgeClass => SourceType switch
    {
        "MustUseNextDay" => "bg-secondary-fixed text-on-secondary-fixed-variant",
        "Reballed" => "bg-primary-fixed text-on-primary-fixed-variant",
        "Attention" => "bg-error-container text-on-error-container",
        _ => "bg-tertiary-fixed text-on-tertiary-fixed-variant"
    };

    public string Guidance => RecommendedAction switch
    {
        "UseFirst" => "Use this source before newer dough so the kitchen clears the highest-priority balls first.",
        "Review" => "This dough still exists physically, but the team should review its condition before the next rush.",
        "Reball" => "Only the remaining live balls are eligible for reball planning from this source.",
        "Discard" => "Manager review is needed before any remaining dough stays in circulation.",
        _ => "No special action is required while this dough remains within the normal service window."
    };
}
