using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDailyDoughClosingManagementService
{
    Task<DailyDoughClosingResponse> CreateDailyClosingAsync(
        CreateDailyDoughClosingRequest request,
        CancellationToken cancellationToken = default);

    Task<DailyDoughClosingResponse> CorrectDailyClosingAsync(
        CorrectDailyDoughClosingRequest request,
        CancellationToken cancellationToken = default);
}
