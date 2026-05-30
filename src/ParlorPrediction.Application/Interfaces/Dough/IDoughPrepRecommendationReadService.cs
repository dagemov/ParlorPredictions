using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughPrepRecommendationReadService
{
    Task<DoughRecommendationDetailResponse?> GetLatestByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
