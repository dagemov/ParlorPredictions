namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityReviewRecordViewModel
{
    public Guid Id { get; set; }

    public DateOnly SourceDate { get; set; }

    public DateTime CreatedOrBalledAt { get; set; }

    public int QuantityBalls { get; set; }

    public string CurrentStatus { get; set; } = string.Empty;

    public string? StatusReason { get; set; }

    public DateTime? AttentionMarkedAt { get; set; }

    public DateTime? ReballedAt { get; set; }

    public DateOnly? MustUseByDate { get; set; }

    public DateTime? DiscardedAt { get; set; }

    public string? DiscardReason { get; set; }

    public string? ManagerNote { get; set; }

    public bool CountsAsAvailable { get; set; }

    public string Title => $"Dough from {SourceDate:dddd}";

    public string AvailabilityText => CountsAsAvailable
        ? "Still counts as available."
        : "No longer counts as available.";
}
