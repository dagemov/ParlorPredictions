using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughBatchRepository : IDoughBatchRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughBatchRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(DoughBatch batch, CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughBatches.AddAsync(batch, cancellationToken).AsTask();
    }

    public Task<DoughBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughBatches.FirstOrDefaultAsync(batch => batch.Id == id, cancellationToken);
    }
}
