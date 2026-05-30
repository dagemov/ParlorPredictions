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
