using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepDashboardReadService
{
    Task<PrepDashboardSummaryResponse> GetSummaryAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default);
}
