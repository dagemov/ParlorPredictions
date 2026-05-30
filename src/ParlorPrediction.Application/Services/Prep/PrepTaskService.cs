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
}
