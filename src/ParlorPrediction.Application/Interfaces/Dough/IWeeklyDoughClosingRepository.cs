using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IWeeklyDoughClosingRepository
{
    Task AddAsync(WeeklyDoughClosing closing, CancellationToken cancellationToken = default);

    Task<WeeklyDoughClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WeeklyDoughClosing?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeeklyDoughClosing>> ListAsync(
        DateOnly? fromWeekStartDate,
        DateOnly? toWeekStartDate,
        CancellationToken cancellationToken = default);
}
