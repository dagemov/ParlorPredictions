namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class ManagerPrepRecommendationListItemViewModel
{
    public Guid Id { get; set; }

    public DateOnly RecommendationDate { get; set; }

    public Guid PrepItemId { get; set; }

    public string PrepItemName { get; set; } = string.Empty;

    public string RecommendationText { get; set; } = string.Empty;

    public int RecommendedBalls { get; set; }

    public int RecommendedCases { get; set; }

    public int RecommendedLoads { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string CreatedByUserName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
