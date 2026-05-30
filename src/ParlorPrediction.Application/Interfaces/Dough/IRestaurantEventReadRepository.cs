using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IRestaurantEventReadRepository
{
    Task<IReadOnlyCollection<RestaurantEvent>> GetByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
