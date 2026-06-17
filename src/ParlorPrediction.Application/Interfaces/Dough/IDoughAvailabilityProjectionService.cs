using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughAvailabilityProjectionService
{
    Task<DoughAvailabilityProjectionResponse> GetWeeklyAvailabilityAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default);
}
