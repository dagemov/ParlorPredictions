using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class RestaurantEventRepository : IRestaurantEventRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public RestaurantEventRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RestaurantEvent restaurantEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.RestaurantEvents.AddAsync(restaurantEvent, cancellationToken);
    }

    public Task<RestaurantEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.RestaurantEvents
            .FirstOrDefaultAsync(restaurantEvent => restaurantEvent.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RestaurantEvent>> GetByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RestaurantEvents
            .AsNoTracking()
            .Where(restaurantEvent => restaurantEvent.EventDate == targetDate && restaurantEvent.IsActive)
            .OrderBy(restaurantEvent => restaurantEvent.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RestaurantEvent>> GetBetweenDatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RestaurantEvents
            .AsNoTracking()
            .Where(restaurantEvent =>
                restaurantEvent.IsActive &&
                restaurantEvent.EventDate >= startDate &&
                restaurantEvent.EventDate <= endDate)
            .OrderBy(restaurantEvent => restaurantEvent.EventDate)
            .ThenBy(restaurantEvent => restaurantEvent.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RestaurantEvent>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? term,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RestaurantEvents
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(restaurantEvent => restaurantEvent.EventDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(restaurantEvent => restaurantEvent.EventDate <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var normalizedTerm = term.Trim();
            query = query.Where(restaurantEvent =>
                restaurantEvent.Name.Contains(normalizedTerm) ||
                (restaurantEvent.Notes != null && restaurantEvent.Notes.Contains(normalizedTerm)));
        }

        if (activeOnly)
        {
            query = query.Where(restaurantEvent => restaurantEvent.IsActive);
        }

        return await query
            .OrderBy(restaurantEvent => restaurantEvent.EventDate)
            .ThenBy(restaurantEvent => restaurantEvent.Name)
            .ToArrayAsync(cancellationToken);
    }
}
