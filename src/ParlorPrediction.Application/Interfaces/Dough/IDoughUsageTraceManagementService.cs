using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Responses.DoughUsage;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughUsageTraceManagementService
{
    Task<DoughUsageTraceResponse> CreateAsync(
        CreateDoughUsageTraceRequest request,
        CancellationToken cancellationToken = default);

    Task<DoughUsageTraceResponse> CorrectAsync(
        CorrectDoughUsageTraceRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        DeleteDoughUsageTraceRequest request,
        CancellationToken cancellationToken = default);
}
