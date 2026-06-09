using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class WeeklyDoughClosingRepository : IWeeklyDoughClosingRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public WeeklyDoughClosingRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WeeklyDoughClosing closing, CancellationToken cancellationToken = default)
    {
        await _dbContext.AddAsync(closing, cancellationToken);
    }

    public Task<WeeklyDoughClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<WeeklyDoughClosing>()
            .FirstOrDefaultAsync(closing => closing.Id == id, cancellationToken);
    }

    public Task<WeeklyDoughClosing?> GetByWeekStartDateAsync(DateOnly weekStartDate, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<WeeklyDoughClosing>()
            .FirstOrDefaultAsync(closing => closing.WeekStartDate == weekStartDate, cancellationToken);
    }

    public async Task<IReadOnlyList<WeeklyDoughClosing>> ListAsync(
        DateOnly? fromWeekStartDate,
        DateOnly? toWeekStartDate,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<WeeklyDoughClosing>()
            .AsNoTracking()
            .AsQueryable();

        if (fromWeekStartDate.HasValue)
        {
            query = query.Where(closing => closing.WeekStartDate >= fromWeekStartDate.Value);
        }

        if (toWeekStartDate.HasValue)
        {
            query = query.Where(closing => closing.WeekStartDate <= toWeekStartDate.Value);
        }

        return await query
            .OrderByDescending(closing => closing.WeekStartDate)
            .ToArrayAsync(cancellationToken);
    }
}
