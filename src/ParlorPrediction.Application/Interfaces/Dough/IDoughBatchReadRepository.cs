using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughBatchReadRepository
{
    Task<IReadOnlyCollection<DoughBatch>> GetProducedOnOrBeforeAsync(
        DateOnly productionDate,
        CancellationToken cancellationToken = default);
}
