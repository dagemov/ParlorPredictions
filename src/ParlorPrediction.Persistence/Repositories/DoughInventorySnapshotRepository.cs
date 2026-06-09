using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughInventorySnapshotRepository : IDoughInventorySnapshotRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughInventorySnapshotRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(DoughInventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughInventorySnapshots.AddAsync(snapshot, cancellationToken).AsTask();
    }

    public Task<DoughInventorySnapshot?> GetLatestOnOrBeforeForUpdateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughInventorySnapshots
            .Where(snapshot => snapshot.SnapshotDate <= targetDate)
            .OrderByDescending(snapshot => snapshot.SnapshotDate)
            .ThenByDescending(snapshot => snapshot.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
