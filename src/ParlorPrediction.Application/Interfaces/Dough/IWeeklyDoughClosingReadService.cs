using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IWeeklyDoughClosingReadService
{
    Task<IReadOnlyList<WeeklyDoughClosingResponse>> GetWeeklyClosingsAsync(
        GetWeeklyClosingsRequest request,
        CancellationToken cancellationToken = default);

    Task<WeeklyDoughCarryoverResponse> GetCarryoverForWeekAsync(
        GetWeeklyDoughCarryoverRequest request,
        CancellationToken cancellationToken = default);
}
