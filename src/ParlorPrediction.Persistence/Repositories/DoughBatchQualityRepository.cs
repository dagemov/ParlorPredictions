using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughBatchQualityRepository : IDoughBatchQualityRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughBatchQualityRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoughBatchQualityRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoughBatchQualityRecords.AddAsync(record, cancellationToken);
    }

    public Task<DoughBatchQualityRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughBatchQualityRecords
            .Include(record => record.LossRecords)
            .Include(record => record.ReballRecords)
            .FirstOrDefaultAsync(record => record.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DoughBatchQualityRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await ApplyOperationalVisibilityFilter(_dbContext.DoughBatchQualityRecords)
            .AsNoTracking()
            .Include(record => record.ReballRecords)
            .OrderByDescending(record => record.SourceDate)
            .ThenByDescending(record => record.CreatedOrBalledAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoughBatchQualityRecord>> SearchAsync(
        DateOnly? sourceDateFrom,
        DateOnly? sourceDateTo,
        DateOnly? createdOrBalledFromDate,
        DateOnly? createdOrBalledToDate,
        DateOnly? reballedFromDate,
        DateOnly? reballedToDate,
        DoughQualityStatus? currentStatus,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyOperationalVisibilityFilter(_dbContext.DoughBatchQualityRecords)
            .AsNoTracking();

        if (sourceDateFrom.HasValue)
        {
            query = query.Where(record => record.SourceDate >= sourceDateFrom.Value);
        }

        if (sourceDateTo.HasValue)
        {
            query = query.Where(record => record.SourceDate <= sourceDateTo.Value);
        }

        if (createdOrBalledFromDate.HasValue)
        {
            var createdFrom = createdOrBalledFromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(record => record.CreatedOrBalledAt >= createdFrom);
        }

        if (createdOrBalledToDate.HasValue)
        {
            var createdToExclusive = createdOrBalledToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(record => record.CreatedOrBalledAt < createdToExclusive);
        }

        if (reballedFromDate.HasValue)
        {
            var reballedFrom = reballedFromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(record => record.ReballedAt.HasValue && record.ReballedAt.Value >= reballedFrom);
        }

        if (reballedToDate.HasValue)
        {
            var reballedToExclusive = reballedToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(record => record.ReballedAt.HasValue && record.ReballedAt.Value < reballedToExclusive);
        }

        if (currentStatus.HasValue)
        {
            query = query.Where(record => record.CurrentStatus == currentStatus.Value);
        }

        return await query
            .OrderByDescending(record => record.SourceDate)
            .ThenByDescending(record => record.CreatedOrBalledAt)
            .ToArrayAsync(cancellationToken);
    }

    private static IQueryable<DoughBatchQualityRecord> ApplyOperationalVisibilityFilter(
        IQueryable<DoughBatchQualityRecord> query)
    {
        return query.Where(record =>
            !record.OriginalDoughTaskId.HasValue ||
            record.OriginalDoughTask == null ||
            record.OriginalDoughTask.Status != PrepTaskStatus.Cancelled);
    }
}
