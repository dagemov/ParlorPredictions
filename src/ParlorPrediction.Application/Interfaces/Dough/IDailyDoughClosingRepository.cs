using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDailyDoughClosingRepository
{
    Task AddAsync(DailyDoughClosing closing, CancellationToken cancellationToken = default);

    Task<DailyDoughClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DailyDoughClosing?> GetByClosingDateAsync(DateOnly closingDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyDoughClosing>> ListByWeekStartDateAsync(
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default);
}
