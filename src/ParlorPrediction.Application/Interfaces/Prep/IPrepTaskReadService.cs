using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepTaskReadService
{
    Task<IReadOnlyList<DoughTaskListItemResponse>> GetDoughTasksByDateAsync(
        DateOnly taskDate,
        CancellationToken cancellationToken = default);

    Task<DoughTaskListItemResponse?> GetByIdAsync(
        Guid prepTaskId,
        CancellationToken cancellationToken = default);
}
