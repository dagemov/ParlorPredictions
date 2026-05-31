using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class PrepItemReadRepository : IPrepItemReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public PrepItemReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PrepItem>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PrepItems
            .AsNoTracking()
            .Include(item => item.PrepStation)
            .Where(item => item.IsActive && item.PrepStation.IsActive)
            .OrderBy(item => item.PrepStation.Name)
            .ThenBy(item => item.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<PrepItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PrepItems
            .AsNoTracking()
            .Include(item => item.PrepStation)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<PrepItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return Task.FromResult<PrepItem?>(null);
        }

        return _dbContext.PrepItems
            .AsNoTracking()
            .Include(item => item.PrepStation)
            .FirstOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken);
    }
}
