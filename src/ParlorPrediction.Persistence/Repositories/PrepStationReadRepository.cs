using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class PrepStationReadRepository : IPrepStationReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public PrepStationReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PrepStation>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PrepStations
            .AsNoTracking()
            .Where(station => station.IsActive)
            .OrderBy(station => station.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<PrepStation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PrepStations
            .AsNoTracking()
            .FirstOrDefaultAsync(station => station.Id == id, cancellationToken);
    }
}
