using ParlorPrediction.Contracts.Requests.Ai;
using ParlorPrediction.Contracts.Responses.Ai;

namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IAiPrepRecommendationService
{
    Task<AiPrepRecommendationResponse> GenerateAsync(
        AiPrepRecommendationRequest request,
        CancellationToken cancellationToken = default);
}
