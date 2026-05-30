using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class RestaurantEventManagementService : IRestaurantEventManagementService
{
    private readonly IRestaurantEventRepository _restaurantEventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestaurantEventManagementService(
        IRestaurantEventRepository restaurantEventRepository,
        IUnitOfWork unitOfWork)
    {
        _restaurantEventRepository = restaurantEventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RestaurantEventListItemResponse>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? term,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var events = await _restaurantEventRepository.SearchAsync(
            fromDate,
            toDate,
            term,
            activeOnly,
            cancellationToken);

        return events
            .Select(MapListItem)
            .ToArray();
    }

    public async Task<RestaurantEventDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var restaurantEvent = await _restaurantEventRepository.GetByIdAsync(id, cancellationToken);
        return restaurantEvent is null ? null : MapDetail(restaurantEvent);
    }

    public async Task<Guid> CreateAsync(
        SaveRestaurantEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restaurantEvent = new RestaurantEvent(
            Guid.NewGuid(),
            request.EventDate,
            request.Name,
            request.EstimatedPizzas,
            request.EstimatedDoughBalls,
            request.AllowShortFermentation,
            request.IsActive,
            request.Notes);

        await _restaurantEventRepository.AddAsync(restaurantEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return restaurantEvent.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        SaveRestaurantEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restaurantEvent = await _restaurantEventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The restaurant event could not be found.");

        restaurantEvent.UpdateEvent(
            request.EventDate,
            request.Name,
            request.EstimatedPizzas,
            request.EstimatedDoughBalls,
            request.AllowShortFermentation,
            request.IsActive,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var restaurantEvent = await _restaurantEventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The restaurant event could not be found.");

        if (isActive)
        {
            restaurantEvent.Activate();
        }
        else
        {
            restaurantEvent.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static RestaurantEventListItemResponse MapListItem(RestaurantEvent restaurantEvent)
    {
        return new RestaurantEventListItemResponse
        {
            Id = restaurantEvent.Id,
            Name = restaurantEvent.Name,
            EventDate = restaurantEvent.EventDate,
            EstimatedPizzas = restaurantEvent.EstimatedPizzas,
            EstimatedDoughBalls = restaurantEvent.EstimatedDoughBalls,
            AllowShortFermentation = restaurantEvent.AllowShortFermentation,
            Notes = restaurantEvent.Notes,
            IsActive = restaurantEvent.IsActive,
            UpdatedAtUtc = restaurantEvent.UpdatedAtUtc
        };
    }

    private static RestaurantEventDetailResponse MapDetail(RestaurantEvent restaurantEvent)
    {
        return new RestaurantEventDetailResponse
        {
            Id = restaurantEvent.Id,
            Name = restaurantEvent.Name,
            EventDate = restaurantEvent.EventDate,
            EstimatedPizzas = restaurantEvent.EstimatedPizzas,
            EstimatedDoughBalls = restaurantEvent.EstimatedDoughBalls,
            AllowShortFermentation = restaurantEvent.AllowShortFermentation,
            Notes = restaurantEvent.Notes,
            IsActive = restaurantEvent.IsActive
        };
    }
}
