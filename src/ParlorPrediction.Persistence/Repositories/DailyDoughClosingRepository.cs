using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DailyDoughClosingRepository : IDailyDoughClosingRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DailyDoughClosingRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DailyDoughClosing closing, CancellationToken cancellationToken = default)
    {
        await _dbContext.AddAsync(closing, cancellationToken);
    }

    public Task<DailyDoughClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<DailyDoughClosing>()
            .FirstOrDefaultAsync(closing => closing.Id == id, cancellationToken);
    }

    public Task<DailyDoughClosing?> GetByClosingDateAsync(DateOnly closingDate, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<DailyDoughClosing>()
            .FirstOrDefaultAsync(closing => closing.ClosingDate == closingDate, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyDoughClosing>> SearchAsync(
        DateOnly? closingDateFrom,
        DateOnly? closingDateTo,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<DailyDoughClosing>()
            .AsNoTracking()
            .AsQueryable();

        if (closingDateFrom.HasValue)
        {
            query = query.Where(closing => closing.ClosingDate >= closingDateFrom.Value);
        }

        if (closingDateTo.HasValue)
        {
            query = query.Where(closing => closing.ClosingDate <= closingDateTo.Value);
        }

        return await query
            .OrderBy(closing => closing.ClosingDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DailyDoughClosing>> ListByWeekStartDateAsync(
        DateOnly weekStartDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<DailyDoughClosing>()
            .AsNoTracking()
            .Where(closing => closing.WeekStartDate == weekStartDate)
            .OrderBy(closing => closing.ClosingDate)
            .ToArrayAsync(cancellationToken);
    }
}
