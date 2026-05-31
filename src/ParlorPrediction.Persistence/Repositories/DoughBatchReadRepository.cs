using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughBatchReadRepository : IDoughBatchReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughBatchReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<DoughBatch>> GetProducedOnOrBeforeAsync(
        DateOnly productionDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DoughBatches
            .AsNoTracking()
            .Where(batch => batch.BatchDate <= productionDate)
            .OrderBy(batch => batch.BatchDate)
            .ThenBy(batch => batch.FermentationReadyDate)
            .ToArrayAsync(cancellationToken);
    }
}
