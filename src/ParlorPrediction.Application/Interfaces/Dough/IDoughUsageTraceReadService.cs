using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Responses.DoughUsage;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughUsageTraceReadService
{
    Task<DoughUsageTraceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughUsageTraceResponse>> SearchAsync(
        SearchDoughUsageTracesRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughUsageSourceOptionResponse>> GetAvailableSourcesForDateAsync(
        GetAvailableDoughSourcesRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughSourceRemainingResponse>> GetRemainingBySourceAsync(
        GetDoughRemainingBySourceRequest request,
        CancellationToken cancellationToken = default);

    Task<DoughReballPlanningResponse> GetReballPlanningForDateAsync(
        GetDoughReballPlanningRequest request,
        CancellationToken cancellationToken = default);
}
