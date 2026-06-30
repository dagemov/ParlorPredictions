using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IInventoryTransformationLedgerRepository
{
    Task AddAsync(InventoryTransformationLedger entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransformationLedger>> ListByOccurredOnRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}
