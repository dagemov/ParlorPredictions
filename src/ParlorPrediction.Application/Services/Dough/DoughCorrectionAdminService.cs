using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughCorrectionAdminService : IDoughCorrectionAdminService
{
    private readonly IDoughBatchRepository _doughBatchRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public DoughCorrectionAdminService(
        IDoughBatchRepository doughBatchRepository,
        IPrepTaskRepository prepTaskRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _doughBatchRepository = doughBatchRepository;
        _prepTaskRepository = prepTaskRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task CorrectPrepTaskAsync(
        AdminCorrectPrepTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredAdminUserAsync(request.UpdatedByUserId, cancellationToken);
        var task = await _prepTaskRepository.GetByIdAsync(request.PrepTaskId, cancellationToken)
            ?? throw new KeyNotFoundException("The prep task could not be found.");

        if (!Enum.TryParse<PrepTaskType>(request.TaskType, ignoreCase: true, out var taskType))
        {
            throw new ArgumentException("The prep task type is not valid.", nameof(request.TaskType));
        }

        if (!Enum.TryParse<DoughQuantityUnit>(request.QuantityUnit, ignoreCase: true, out var quantityUnit))
        {
            throw new ArgumentException("The dough quantity unit is not valid.", nameof(request.QuantityUnit));
        }

        if (!Enum.TryParse<PrepTaskStatus>(request.Status, ignoreCase: true, out var status))
        {
            throw new ArgumentException("The prep task status is not valid.", nameof(request.Status));
        }

        var completedByUserId = status == PrepTaskStatus.Completed
            ? string.IsNullOrWhiteSpace(request.CompletedByUserId)
                ? task.CompletedByUserId ?? user.Id
                : request.CompletedByUserId
            : null;

        task.AdminCorrect(
            request.TaskDate,
            taskType,
            quantityUnit,
            request.QuantityRecommended,
            status,
            request.QuantityCompleted,
            request.CompletedAtUtc,
            completedByUserId,
            request.SourcePrepTaskId,
            request.SourceDoughBatchId,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CorrectDoughBatchAsync(
        AdminCorrectDoughBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await GetRequiredAdminUserAsync(request.UpdatedByUserId, cancellationToken);
        var batch = await _doughBatchRepository.GetByIdAsync(request.DoughBatchId, cancellationToken)
            ?? throw new KeyNotFoundException("The dough batch could not be found.");

        batch.CorrectBatch(
            request.BatchDate,
            request.TotalCases,
            request.IsBalled,
            request.BalledAtUtc,
            request.IsEventException,
            request.Notes);

        if (request.IsVoided)
        {
            batch.Void(request.VoidReason);
        }
        else
        {
            batch.Restore();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetRequiredAdminUserAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("The acting user could not be found or is inactive.");
        }

        if (user.Role != ApplicationRole.Admin)
        {
            throw new InvalidOperationException("Only admin users can perform destructive dough corrections.");
        }

        return user;
    }
}
