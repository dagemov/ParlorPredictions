using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IRestaurantEventRepository : IRestaurantEventReadRepository
{
    Task AddAsync(RestaurantEvent restaurantEvent, CancellationToken cancellationToken = default);

    Task<RestaurantEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RestaurantEvent>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? term,
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
