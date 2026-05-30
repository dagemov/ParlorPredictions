using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IRestaurantEventManagementService
{
    Task<IReadOnlyList<RestaurantEventListItemResponse>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? term,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<RestaurantEventDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        SaveRestaurantEventRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        SaveRestaurantEventRequest request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
