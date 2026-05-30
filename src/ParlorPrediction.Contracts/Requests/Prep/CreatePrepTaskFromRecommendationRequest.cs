namespace ParlorPrediction.Contracts.Requests.Prep;

public sealed class CreatePrepTaskFromRecommendationRequest
{
    public Guid DoughPrepRecommendationId { get; set; }
}
