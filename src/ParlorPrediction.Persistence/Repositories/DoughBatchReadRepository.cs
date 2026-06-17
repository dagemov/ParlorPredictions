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
            .Where(batch => batch.BatchDate <= productionDate && !batch.IsVoided)
            .OrderBy(batch => batch.BatchDate)
            .ThenBy(batch => batch.FermentationReadyDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DoughBatch>> SearchForCorrectionAsync(
        DateOnly? batchDateFrom,
        DateOnly? batchDateTo,
        bool includeVoided,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DoughBatches
            .AsNoTracking()
            .AsQueryable();

        if (batchDateFrom.HasValue)
        {
            query = query.Where(batch => batch.BatchDate >= batchDateFrom.Value);
        }

        if (batchDateTo.HasValue)
        {
            query = query.Where(batch => batch.BatchDate <= batchDateTo.Value);
        }

        if (!includeVoided)
        {
            query = query.Where(batch => !batch.IsVoided);
        }

        return await query
            .OrderByDescending(batch => batch.BatchDate)
            .ThenByDescending(batch => batch.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
