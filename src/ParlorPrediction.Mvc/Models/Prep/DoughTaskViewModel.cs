using ParlorPrediction.Mvc.Helpers;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughTaskViewModel
{
    public Guid PrepTaskId { get; set; }

    public Guid? DoughPrepRecommendationId { get; set; }

    public DateOnly TaskDate { get; set; }

    public Guid PrepItemId { get; set; }

    public string PrepItemCode { get; set; } = string.Empty;

    public Guid PrepStationId { get; set; }

    public string PrepStationCode { get; set; } = string.Empty;

    public int HistoricalWeeksToUse { get; set; } = 8;

    public string PrepItemName { get; set; } = string.Empty;

    public string PrepStationName { get; set; } = string.Empty;

    public string AssignedRole { get; set; } = string.Empty;

    public int QuantityRecommended { get; set; }

    public int QuantityCompleted { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string? CompletedByUserId { get; set; }

    public string? CompletedByUserName { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsManualTask { get; set; }

    public bool CanComplete { get; set; }

    public bool CanManage { get; set; }

    public string CompletionPreviewText => DoughQuantityInputConverter.BuildCompletedPreviewText("Balls", Math.Max(QuantityRecommended - QuantityCompleted, 1));
}
