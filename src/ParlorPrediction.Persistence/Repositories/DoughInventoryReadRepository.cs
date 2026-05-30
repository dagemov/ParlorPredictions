using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughInventoryReadRepository : IDoughInventoryReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughInventoryReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DoughInventorySnapshot?> GetLatestSnapshotOnOrBeforeAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughInventorySnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.SnapshotDate <= targetDate)
            .OrderByDescending(snapshot => snapshot.SnapshotDate)
            .ThenByDescending(snapshot => snapshot.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
