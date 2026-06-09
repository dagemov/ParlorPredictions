using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughLossRecordRepository : IDoughLossRecordRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughLossRecordRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoughLossRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoughLossRecords.AddAsync(record, cancellationToken);
    }

    public async Task<IReadOnlyList<DoughLossRecord>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        DoughLossReason? lossReason,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DoughLossRecords
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(record => record.LossDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(record => record.LossDate <= toDate.Value);
        }

        if (lossReason.HasValue)
        {
            query = query.Where(record => record.LossReason == lossReason.Value);
        }

        return await query
            .OrderBy(record => record.LossDate)
            .ThenBy(record => record.LossReason)
            .ThenBy(record => record.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
