using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughInventorySnapshotRepository
{
    Task AddAsync(DoughInventorySnapshot snapshot, CancellationToken cancellationToken = default);

    Task<DoughInventorySnapshot?> GetLatestOnOrBeforeForUpdateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
