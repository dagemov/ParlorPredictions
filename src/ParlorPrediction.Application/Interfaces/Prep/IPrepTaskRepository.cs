using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepTaskRepository
{
    Task AddAsync(PrepTask task, CancellationToken cancellationToken = default);

    Task<PrepTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PrepTask?> GetByDoughPrepRecommendationIdAsync(Guid doughPrepRecommendationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrepTask>> GetDoughTasksByDateAsync(DateOnly taskDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrepTask>> GetDoughTasksBetweenDatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
