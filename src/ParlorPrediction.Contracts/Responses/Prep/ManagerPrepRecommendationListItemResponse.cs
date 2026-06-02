namespace ParlorPrediction.Contracts.Responses.Prep;

public sealed class ManagerPrepRecommendationListItemResponse
{
    public Guid Id { get; init; }

    public DateOnly RecommendationDate { get; init; }

    public Guid PrepItemId { get; init; }

    public string PrepItemName { get; init; } = string.Empty;

    public string RecommendationText { get; init; } = string.Empty;

    public int RecommendedBalls { get; init; }

    public int RecommendedCases { get; init; }

    public int RecommendedLoads { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string CreatedByUserId { get; init; } = string.Empty;

    public string CreatedByUserName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}
