using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Constants;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class PrepTaskRepository : IPrepTaskRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public PrepTaskRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PrepTask task, CancellationToken cancellationToken = default)
    {
        await _dbContext.PrepTasks.AddAsync(task, cancellationToken);
    }

    public Task<PrepTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PrepTasks
            .Include(task => task.PrepItem)
            .Include(task => task.PrepStation)
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public Task<PrepTask?> GetByDoughPrepRecommendationIdAsync(Guid doughPrepRecommendationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PrepTasks
            .Include(task => task.PrepItem)
            .Include(task => task.PrepStation)
            .FirstOrDefaultAsync(task => task.DoughPrepRecommendationId == doughPrepRecommendationId, cancellationToken);
    }

    public async Task<IReadOnlyList<PrepTask>> GetDoughTasksByDateAsync(
        DateOnly taskDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PrepTasks
            .AsNoTracking()
            .Include(task => task.PrepItem)
            .Include(task => task.PrepStation)
            .Where(task =>
                task.TaskDate == taskDate &&
                task.PrepItem.Code == PrepCatalogCodes.DoughItem)
            .OrderBy(task => task.Status)
            .ThenBy(task => task.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrepTask>> GetDoughTasksBetweenDatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PrepTasks
            .AsNoTracking()
            .Include(task => task.PrepItem)
            .Include(task => task.PrepStation)
            .Where(task =>
                task.TaskDate >= startDate &&
                task.TaskDate <= endDate &&
                task.PrepItem.Code == PrepCatalogCodes.DoughItem)
            .OrderBy(task => task.TaskDate)
            .ThenBy(task => task.Status)
            .ThenBy(task => task.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
