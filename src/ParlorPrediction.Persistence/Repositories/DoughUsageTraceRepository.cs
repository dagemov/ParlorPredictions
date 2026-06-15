using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughUsageTraceRepository : IDoughUsageTraceRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughUsageTraceRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoughUsageTrace trace, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoughUsageTraces.AddAsync(trace, cancellationToken);
    }

    public Task<DoughUsageTrace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughUsageTraces
            .FirstOrDefaultAsync(trace => trace.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DoughUsageTrace>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DoughUsageTraces
            .AsNoTracking()
            .OrderByDescending(trace => trace.UsageDate)
            .ThenByDescending(trace => trace.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoughUsageTrace>> SearchAsync(
        DateOnly? usageDateFrom,
        DateOnly? usageDateTo,
        Guid? sourceDoughBatchQualityRecordId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DoughUsageTraces
            .AsNoTracking()
            .AsQueryable();

        if (usageDateFrom.HasValue)
        {
            query = query.Where(trace => trace.UsageDate >= usageDateFrom.Value);
        }

        if (usageDateTo.HasValue)
        {
            query = query.Where(trace => trace.UsageDate <= usageDateTo.Value);
        }

        if (sourceDoughBatchQualityRecordId.HasValue)
        {
            query = query.Where(trace => trace.SourceDoughBatchQualityRecordId == sourceDoughBatchQualityRecordId.Value);
        }

        return await query
            .OrderByDescending(trace => trace.UsageDate)
            .ThenByDescending(trace => trace.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public void Remove(DoughUsageTrace trace)
    {
        _dbContext.DoughUsageTraces.Remove(trace);
    }
}
