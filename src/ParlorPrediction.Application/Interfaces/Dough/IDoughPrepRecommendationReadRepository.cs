using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughPrepRecommendationReadRepository
{
    Task<DoughPrepRecommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DoughPrepRecommendation?> GetLatestByDateAsync(DateOnly targetDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughPrepRecommendation>> GetLatestBetweenDatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
