using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Constants;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

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
            .Include(task => task.CompletedByUser)
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
            .Include(task => task.CompletedByUser)
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
            .Include(task => task.CompletedByUser)
            .Where(task =>
                task.TaskDate >= startDate &&
                task.TaskDate <= endDate &&
                task.PrepItem.Code == PrepCatalogCodes.DoughItem)
            .OrderBy(task => task.TaskDate)
            .ThenBy(task => task.Status)
            .ThenBy(task => task.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrepTask>> SearchDoughTasksAsync(
        DateOnly? taskDate,
        PrepTaskStatus? status,
        ApplicationRole? assignedRole,
        Guid? prepItemId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PrepTasks
            .AsNoTracking()
            .Include(task => task.PrepItem)
            .Include(task => task.PrepStation)
            .Include(task => task.CompletedByUser)
            .Where(task => task.PrepItem.Code == PrepCatalogCodes.DoughItem)
            .AsQueryable();

        if (taskDate.HasValue)
        {
            query = query.Where(task => task.TaskDate == taskDate.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(task => task.Status == status.Value);
        }

        if (assignedRole.HasValue)
        {
            query = query.Where(task => task.AssignedRole == assignedRole.Value);
        }

        if (prepItemId.HasValue && prepItemId.Value != Guid.Empty)
        {
            query = query.Where(task => task.PrepItemId == prepItemId.Value);
        }

        return await query
            .OrderBy(task => task.TaskDate)
            .ThenBy(task => task.Status)
            .ThenBy(task => task.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public void Remove(PrepTask task)
    {
        _dbContext.PrepTasks.Remove(task);
    }
}
