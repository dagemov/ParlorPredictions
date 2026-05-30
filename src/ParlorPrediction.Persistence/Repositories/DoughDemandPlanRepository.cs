using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughDemandPlanRepository : IDoughDemandPlanRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughDemandPlanRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoughDemandPlan doughDemandPlan, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoughDemandPlans.AddAsync(doughDemandPlan, cancellationToken);
    }

    public Task<DoughDemandPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughDemandPlans
            .FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DoughDemandPlan>> GetActiveByDayOfWeekAsync(
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DoughDemandPlans
            .AsNoTracking()
            .Where(plan => plan.DayOfWeek == dayOfWeek && plan.IsActive)
            .OrderBy(plan => plan.SourceName)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DoughDemandPlan>> SearchAsync(
        DayOfWeek? dayOfWeek,
        string? sourceTerm,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DoughDemandPlans
            .AsNoTracking()
            .AsQueryable();

        if (dayOfWeek.HasValue)
        {
            query = query.Where(plan => plan.DayOfWeek == dayOfWeek.Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceTerm))
        {
            var normalizedSourceTerm = sourceTerm.Trim();
            query = query.Where(plan => plan.SourceName.Contains(normalizedSourceTerm));
        }

        if (activeOnly)
        {
            query = query.Where(plan => plan.IsActive);
        }

        return await query
            .OrderBy(plan => plan.DayOfWeek)
            .ThenBy(plan => plan.SourceName)
            .ToArrayAsync(cancellationToken);
    }
}
