using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IConsumptionLedgerRepository
{
    Task AddAsync(ConsumptionLedger entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsumptionLedger>> ListByOccurredOnRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}
