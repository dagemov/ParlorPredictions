using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Constants;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepTaskService : IPrepTaskService
{
    private readonly IDoughPrepRecommendationReadRepository _doughPrepRecommendationReadRepository;
    private readonly IPrepItemReadRepository _prepItemReadRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public PrepTaskService(
        IDoughPrepRecommendationReadRepository doughPrepRecommendationReadRepository,
        IPrepItemReadRepository prepItemReadRepository,
        IPrepTaskRepository prepTaskRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _doughPrepRecommendationReadRepository = doughPrepRecommendationReadRepository;
        _prepItemReadRepository = prepItemReadRepository;
        _prepTaskRepository = prepTaskRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<CreatePrepTaskFromRecommendationResponse> CreateFromDoughRecommendationAsync(
        CreatePrepTaskFromRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.DoughPrepRecommendationId == Guid.Empty)
        {
            throw new ArgumentException("Dough prep recommendation id is required.", nameof(request));
        }

        var recommendation = await _doughPrepRecommendationReadRepository.GetByIdAsync(
            request.DoughPrepRecommendationId,
            cancellationToken);

        if (recommendation is null)
        {
            throw new KeyNotFoundException("The dough prep recommendation was not found.");
        }

        var existingTask = await _prepTaskRepository.GetByDoughPrepRecommendationIdAsync(
            request.DoughPrepRecommendationId,
            cancellationToken);

        if (existingTask is not null)
        {
            return MapCreateResponse(
                existingTask,
                taskCreated: false,
                message: "A prep task already exists for this dough recommendation.");
        }

        if (recommendation.MissingBalls == 0)
        {
            return new CreatePrepTaskFromRecommendationResponse
            {
                TaskCreated = false,
                DoughPrepRecommendationId = recommendation.Id,
                TaskDate = recommendation.RecommendationDate,
                QuantityRecommended = 0,
                Message = "No prep task was created because the recommendation has no missing dough balls."
            };
        }

        var doughItem = await _prepItemReadRepository.GetByCodeAsync(PrepCatalogCodes.DoughItem, cancellationToken);
        if (doughItem is null)
        {
            throw new InvalidOperationException("The DOUGH prep item is not configured.");
        }

        if (!string.Equals(doughItem.PrepStation.Code, PrepCatalogCodes.PizzaStation, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The DOUGH prep item must belong to the PIZZA station.");
        }

        var task = PrepTask.Create(
            recommendation.RecommendationDate,
            doughItem.Id,
            doughItem.PrepStationId,
            ApplicationRole.PizzaMaker,
            recommendation.MissingBalls,
            recommendation.Id);

        await _prepTaskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatePrepTaskFromRecommendationResponse
        {
            TaskCreated = true,
            PrepTaskId = task.Id,
            DoughPrepRecommendationId = recommendation.Id,
            TaskDate = task.TaskDate,
            PrepItemName = doughItem.Name,
            PrepStationName = doughItem.PrepStation.Name,
            AssignedRole = task.AssignedRole.GetCanonicalName(),
            QuantityRecommended = task.QuantityRecommended,
            Status = task.Status.ToString(),
            Message = "Prep task created successfully from dough recommendation."
        };
    }

    public async Task<SavePrepTaskResponse> CreateManualAsync(
        SavePrepTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prepItem = await GetValidatedPrepItemAsync(request.PrepItemId, request.PrepStationId, cancellationToken);
        var assignedRole = ParseAssignableRole(request.AssignedRole);

        var task = PrepTask.Create(
            request.TaskDate,
            prepItem.Id,
            prepItem.PrepStationId,
            assignedRole,
            request.QuantityRecommended,
            notes: request.Notes);

        await _prepTaskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapSaveResponse(task, prepItem.Name, prepItem.PrepStation.Name, "Prep task created successfully.");
    }

    public async Task<SavePrepTaskResponse> UpdateManualAsync(
        Guid prepTaskId,
        SavePrepTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (prepTaskId == Guid.Empty)
        {
            throw new ArgumentException("Prep task id is required.", nameof(prepTaskId));
        }

        var task = await _prepTaskRepository.GetByIdAsync(prepTaskId, cancellationToken)
            ?? throw new KeyNotFoundException("The prep task could not be found.");

        var prepItem = await GetValidatedPrepItemAsync(request.PrepItemId, request.PrepStationId, cancellationToken);
        var assignedRole = ParseAssignableRole(request.AssignedRole);

        task.UpdateTask(
            request.TaskDate,
            prepItem.Id,
            prepItem.PrepStationId,
            assignedRole,
            request.QuantityRecommended,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapSaveResponse(task, prepItem.Name, prepItem.PrepStation.Name, "Prep task updated successfully.");
    }

    public async Task<CompletePrepTaskResponse> CompleteAsync(
        CompletePrepTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PrepTaskId == Guid.Empty)
        {
            throw new ArgumentException("Prep task id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.CompletedByUserId))
        {
            throw new ArgumentException("Completed by user id is required.", nameof(request));
        }

        if (request.QuantityCompleted <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.QuantityCompleted),
                "Quantity completed must be greater than zero.");
        }

        var task = await _prepTaskRepository.GetByIdAsync(request.PrepTaskId, cancellationToken);
        if (task is null)
        {
            throw new KeyNotFoundException("The prep task was not found.");
        }

        var user = await _userRepository.FindByIdAsync(request.CompletedByUserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("The completing user was not found or is inactive.");
        }

        task.Complete(user.Id, request.QuantityCompleted, request.Notes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompletePrepTaskResponse
        {
            PrepTaskId = task.Id,
            Status = task.Status.ToString(),
            QuantityCompleted = task.QuantityCompleted,
            CompletedAtUtc = task.CompletedAtUtc!.Value,
            Message = "Prep task completed successfully."
        };
    }

    public async Task DeleteAsync(
        Guid prepTaskId,
        CancellationToken cancellationToken = default)
    {
        if (prepTaskId == Guid.Empty)
        {
            throw new ArgumentException("Prep task id is required.", nameof(prepTaskId));
        }

        var task = await _prepTaskRepository.GetByIdAsync(prepTaskId, cancellationToken)
            ?? throw new KeyNotFoundException("The prep task could not be found.");

        _prepTaskRepository.Remove(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static CreatePrepTaskFromRecommendationResponse MapCreateResponse(
        PrepTask task,
        bool taskCreated,
        string message)
    {
        return new CreatePrepTaskFromRecommendationResponse
        {
            TaskCreated = taskCreated,
            PrepTaskId = task.Id,
            DoughPrepRecommendationId = task.DoughPrepRecommendationId ?? Guid.Empty,
            TaskDate = task.TaskDate,
            PrepItemName = task.PrepItem.Name,
            PrepStationName = task.PrepStation.Name,
            AssignedRole = task.AssignedRole.GetCanonicalName(),
            QuantityRecommended = task.QuantityRecommended,
            Status = task.Status.ToString(),
            Message = message
        };
    }

    private async Task<PrepItem> GetValidatedPrepItemAsync(
        Guid prepItemId,
        Guid prepStationId,
        CancellationToken cancellationToken)
    {
        if (prepItemId == Guid.Empty)
        {
            throw new ArgumentException("Prep item is required.", nameof(prepItemId));
        }

        if (prepStationId == Guid.Empty)
        {
            throw new ArgumentException("Prep station is required.", nameof(prepStationId));
        }

        var prepItem = await _prepItemReadRepository.GetByIdAsync(prepItemId, cancellationToken)
            ?? throw new KeyNotFoundException("The prep item could not be found.");

        if (!prepItem.IsActive)
        {
            throw new InvalidOperationException("The selected prep item is inactive.");
        }

        if (prepItem.PrepStationId != prepStationId)
        {
            throw new InvalidOperationException("The selected prep item does not belong to the selected station.");
        }

        return prepItem;
    }

    private static ApplicationRole ParseAssignableRole(string assignedRole)
    {
        if (!ApplicationRoleExtensions.TryParse(assignedRole, out var parsedRole) || parsedRole == ApplicationRole.Pending)
        {
            throw new ArgumentException("Assigned role is not valid for prep tasks.", nameof(assignedRole));
        }

        return parsedRole;
    }

    private static SavePrepTaskResponse MapSaveResponse(
        PrepTask task,
        string prepItemName,
        string prepStationName,
        string message)
    {
        return new SavePrepTaskResponse
        {
            PrepTaskId = task.Id,
            TaskDate = task.TaskDate,
            PrepItemName = prepItemName,
            PrepStationName = prepStationName,
            AssignedRole = task.AssignedRole.GetCanonicalName(),
            QuantityRecommended = task.QuantityRecommended,
            Status = task.Status.ToString(),
            Message = message
        };
    }
}
