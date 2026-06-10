using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDailyDoughClosingReadService
{
    Task<DailyDoughClosingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DailyClosingWeekSummaryResponse> GetWeekSummaryAsync(
        GetDailyClosingWeekSummaryRequest request,
        CancellationToken cancellationToken = default);

    Task<DailyClosingOperationalInsightsResponse> GetOperationalInsightsAsync(
        GetDailyClosingWeekSummaryRequest request,
        CancellationToken cancellationToken = default);
}
