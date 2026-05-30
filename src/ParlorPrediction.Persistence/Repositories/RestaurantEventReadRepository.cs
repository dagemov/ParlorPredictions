using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class RestaurantEventReadRepository : IRestaurantEventReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public RestaurantEventReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RestaurantEvent>> GetByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RestaurantEvents
            .AsNoTracking()
            .Where(restaurantEvent => restaurantEvent.EventDate == targetDate)
            .OrderBy(restaurantEvent => restaurantEvent.Name)
            .ToListAsync(cancellationToken);
    }
}
