namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughTaskViewModel
{
    public Guid PrepTaskId { get; set; }

    public Guid? DoughPrepRecommendationId { get; set; }

    public DateOnly TaskDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public string PrepItemName { get; set; } = string.Empty;

    public string PrepStationName { get; set; } = string.Empty;

    public string AssignedRole { get; set; } = string.Empty;

    public int QuantityRecommended { get; set; }

    public int QuantityCompleted { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? CompletedAtUtc { get; set; }

    public bool CanComplete { get; set; }
}
