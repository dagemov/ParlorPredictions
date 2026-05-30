using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughPrepRecommendationRepository
{
    Task AddAsync(DoughPrepRecommendation recommendation, CancellationToken cancellationToken = default);
}
