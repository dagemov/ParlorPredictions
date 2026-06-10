using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Prep;
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

    public async Task<IReadOnlyList<DoughTaskListItemResponse>> SearchAsync(
        SearchPrepTasksRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hasStatus = Enum.TryParse<PrepTaskStatus>(request.Status, true, out var parsedStatus);
        var hasAssignedRole = ApplicationRoleExtensions.TryParse(request.AssignedRole, out var parsedRole);

        var tasks = await _prepTaskRepository.SearchDoughTasksAsync(
            request.TaskDate,
            hasStatus ? parsedStatus : null,
            hasAssignedRole ? parsedRole : null,
            request.PrepItemId,
            cancellationToken);

        return tasks
            .Select(Map)
            .ToArray();
    }

    private static DoughTaskListItemResponse Map(PrepTask task)
    {
        return new DoughTaskListItemResponse
        {
            PrepTaskId = task.Id,
            DoughPrepRecommendationId = task.DoughPrepRecommendationId,
            TaskDate = task.TaskDate,
            PrepItemId = task.PrepItemId,
            PrepItemName = task.PrepItem.Name,
            PrepItemCode = task.PrepItem.Code,
            PrepStationId = task.PrepStationId,
            PrepStationName = task.PrepStation.Name,
            PrepStationCode = task.PrepStation.Code,
            AssignedRole = task.AssignedRole.GetCanonicalName(),
            TaskType = task.TaskType.ToString(),
            QuantityUnit = task.QuantityUnit.ToString(),
            QuantityRecommended = task.QuantityRecommended,
            QuantityCompleted = task.QuantityCompleted,
            QuantityRecommendedBallsEquivalent = task.RecommendedBallsEquivalent,
            QuantityCompletedBallsEquivalent = task.CompletedBallsEquivalent,
            CountsAsAvailableBallsWhenCompleted = task.CountsAsAvailableBallsWhenCompleted,
            SourcePrepTaskId = task.SourcePrepTaskId,
            SourceDoughBatchId = task.SourceDoughBatchId,
            Status = task.Status.ToString(),
            Notes = task.Notes,
            CompletedByUserId = task.CompletedByUserId,
            CompletedByUserName = task.CompletedByUser?.FullName,
            CompletedAtUtc = task.CompletedAtUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            IsManualTask = !task.DoughPrepRecommendationId.HasValue
        };
    }
}
