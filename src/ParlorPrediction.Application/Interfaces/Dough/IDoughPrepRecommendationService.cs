using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughPrepRecommendationService
{
    Task<GenerateDoughPrepRecommendationResponse> GenerateAsync(
        GenerateDoughPrepRecommendationRequest request,
        CancellationToken cancellationToken = default);
}
