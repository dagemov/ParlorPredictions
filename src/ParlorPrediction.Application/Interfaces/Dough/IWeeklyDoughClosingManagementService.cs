using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IWeeklyDoughClosingManagementService
{
    Task<WeeklyDoughClosingResponse> CreateWeeklyClosingAsync(
        CreateWeeklyDoughClosingRequest request,
        CancellationToken cancellationToken = default);

    Task<WeeklyDoughClosingResponse> CorrectWeeklyClosingAsync(
        CorrectWeeklyDoughClosingRequest request,
        CancellationToken cancellationToken = default);
}
