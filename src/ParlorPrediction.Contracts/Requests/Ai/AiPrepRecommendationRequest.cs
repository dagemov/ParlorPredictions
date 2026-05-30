namespace ParlorPrediction.Contracts.Requests.Ai;

public sealed class AiPrepRecommendationRequest
{
    public DateOnly TargetDate { get; init; }
}
