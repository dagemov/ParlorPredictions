using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepTaskService
{
    Task<CreatePrepTaskFromRecommendationResponse> CreateFromDoughRecommendationAsync(
        CreatePrepTaskFromRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<CompletePrepTaskResponse> CompleteAsync(
        CompletePrepTaskRequest request,
        CancellationToken cancellationToken = default);
}
