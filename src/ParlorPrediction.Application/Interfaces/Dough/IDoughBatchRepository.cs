using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughBatchRepository
{
    Task AddAsync(DoughBatch batch, CancellationToken cancellationToken = default);

    Task<DoughBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
