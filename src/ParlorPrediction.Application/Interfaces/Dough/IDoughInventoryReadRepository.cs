using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughInventoryReadRepository
{
    Task<DoughInventorySnapshot?> GetLatestSnapshotOnOrBeforeAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
