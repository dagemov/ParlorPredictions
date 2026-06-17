using ParlorPrediction.Contracts.Responses.DoughUsage;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughSourceProjectionService
{
    Task<IReadOnlyList<DoughSourceRemainingResponse>> GetRemainingBySourceAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughSourceRemainingResponse>> GetTraceableRemainingBySourceAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default);
}
