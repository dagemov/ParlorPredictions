namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class MarkAttentionFromReviewViewModel
{
    public Guid DoughBatchQualityRecordId { get; set; }

    public string StatusReason { get; set; } = string.Empty;

    public DateOnly ReferenceDate { get; set; }

    public DateOnly? CreatedOrBalledFromDate { get; set; }

    public DateOnly? CreatedOrBalledToDate { get; set; }

    public DateOnly? ReballedFromDate { get; set; }

    public DateOnly? ReballedToDate { get; set; }

    public string? CurrentStatus { get; set; }
}
