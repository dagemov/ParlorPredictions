namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityReviewCandidateViewModel
{
    public Guid DoughBatchQualityRecordId { get; set; }

    public DateOnly SourceDate { get; set; }

    public DateTime CreatedOrBalledAt { get; set; }

    public int QuantityBalls { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;

    public int AgeDays { get; set; }

    public string CandidateReason { get; set; } = string.Empty;

    public string Title => $"Dough from {SourceDate:dddd}";

    public string AvailabilityText =>
        string.Equals(CurrentStatus, "Discarded", StringComparison.OrdinalIgnoreCase)
            ? "No longer counts as available."
            : "Still counts as available.";
}
