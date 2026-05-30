using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepTaskReadService : IPrepTaskReadService
{
    private readonly IPrepTaskRepository _prepTaskRepository;

    public PrepTaskReadService(IPrepTaskRepository prepTaskRepository)
    {
        _prepTaskRepository = prepTaskRepository;
    }

    public async Task<IReadOnlyList<DoughTaskListItemResponse>> GetDoughTasksByDateAsync(
        DateOnly taskDate,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _prepTaskRepository.GetDoughTasksByDateAsync(taskDate, cancellationToken);

        return tasks
            .Select(Map)
            .ToArray();
    }

    public async Task<DoughTaskListItemResponse?> GetByIdAsync(
        Guid prepTaskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _prepTaskRepository.GetByIdAsync(prepTaskId, cancellationToken);
        return task is null ? null : Map(task);
    }

    private static DoughTaskListItemResponse Map(PrepTask task)
    {
        return new DoughTaskListItemResponse
        {
            PrepTaskId = task.Id,
            DoughPrepRecommendationId = task.DoughPrepRecommendationId,
            TaskDate = task.TaskDate,
            PrepItemName = task.PrepItem.Name,
            PrepStationName = task.PrepStation.Name,
            AssignedRole = task.AssignedRole.GetCanonicalName(),
            QuantityRecommended = task.QuantityRecommended,
            QuantityCompleted = task.QuantityCompleted,
            Status = task.Status.ToString(),
            CompletedAtUtc = task.CompletedAtUtc
        };
    }
}
