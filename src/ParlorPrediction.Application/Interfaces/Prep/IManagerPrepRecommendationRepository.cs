using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IManagerPrepRecommendationRepository
{
    Task AddAsync(ManagerPrepRecommendation recommendation, CancellationToken cancellationToken = default);

    Task<ManagerPrepRecommendation?> GetLatestByPrepItemOnOrBeforeDateAsync(
        Guid prepItemId,
        DateOnly recommendationDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagerPrepRecommendation>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? prepItemId,
        CancellationToken cancellationToken = default);
}
