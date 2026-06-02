using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepTaskService
{
    Task<CreatePrepTaskFromRecommendationResponse> CreateFromDoughRecommendationAsync(
        CreatePrepTaskFromRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<SavePrepTaskResponse> CreateManualAsync(
        SavePrepTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<SavePrepTaskResponse> UpdateManualAsync(
        Guid prepTaskId,
        SavePrepTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<CompletePrepTaskResponse> CompleteAsync(
        CompletePrepTaskRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid prepTaskId,
        CancellationToken cancellationToken = default);
}
