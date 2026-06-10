using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughBatchQualityRepository
{
    Task AddAsync(DoughBatchQualityRecord record, CancellationToken cancellationToken = default);

    Task<DoughBatchQualityRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughBatchQualityRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughBatchQualityRecord>> SearchAsync(
        DateOnly? sourceDateFrom,
        DateOnly? sourceDateTo,
        DateOnly? createdOrBalledFromDate,
        DateOnly? createdOrBalledToDate,
        DateOnly? reballedFromDate,
        DateOnly? reballedToDate,
        DoughQualityStatus? currentStatus,
        CancellationToken cancellationToken = default);
}
