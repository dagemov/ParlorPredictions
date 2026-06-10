using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughReballRecordRepository : IDoughReballRecordRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughReballRecordRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoughReballRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoughReballRecords.AddAsync(record, cancellationToken);
    }
}
