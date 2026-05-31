using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IManagerPrepRecommendationService
{
    Task<Guid> CreateAsync(
        SaveManagerPrepRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagerPrepRecommendationListItemResponse>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? prepItemId,
        int take = 12,
        CancellationToken cancellationToken = default);
}
