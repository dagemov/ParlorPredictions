using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IProductionLedgerRepository
{
    Task AddAsync(ProductionLedger entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionLedger>> ListByOccurredOnRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}
