namespace ParlorPrediction.Contracts.Requests.Prep;

public sealed class SaveManagerPrepRecommendationRequest
{
    public DateOnly RecommendationDate { get; init; }

    public Guid PrepItemId { get; init; }

    public string RecommendationText { get; init; } = string.Empty;

    public int RecommendedBalls { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string CreatedByUserId { get; init; } = string.Empty;
}
