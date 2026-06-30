using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Constants;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepTaskService : IPrepTaskService
{
    private const string MakeDoughLoadLedgerSourceType = "MakeDoughLoad";
    private const string BallDoughLedgerSourceType = "BallDough";

    private readonly IDoughBatchRepository _doughBatchRepository;
    private readonly IDoughBatchQualityRepository _doughBatchQualityRepository;
    private readonly IDoughInventorySnapshotRepository _doughInventorySnapshotRepository;
    private readonly IDoughPrepRecommendationReadRepository _doughPrepRecommendationReadRepository;
    private readonly IPrepItemReadRepository _prepItemReadRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IProductionLedgerRepository? _productionLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public PrepTaskService(
        IDoughBatchRepository doughBatchRepository,
        IDoughBatchQualityRepository doughBatchQualityRepository,
        IDoughInventorySnapshotRepository doughInventorySnapshotRepository,
        IDoughPrepRecommendationReadRepository doughPrepRecommendationReadRepository,
        IPrepItemReadRepository prepItemReadRepository,
        IPrepTaskRepository prepTaskRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IProductionLedgerRepository? productionLedgerRepository = null)
    {
        _doughBatchRepository = doughBatchRepository;
        _doughBatchQualityRepository = doughBatchQualityRepository;
        _doughInventorySnapshotRepository = doughInventorySnapshotRepository;
        _doughPrepRecommendationReadRepository = doughPrepRecommendationReadRepository;
        _prepItemReadRepository = prepItemReadRepository;
        _prepTaskRepository = prepTaskRepository;
        _productionLedgerRepository = productionLedgerRepository;
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
                QuantityRecommendedBallsEquivalent = 0,
                TaskType = PrepTaskType.MakeDoughLoad.ToString(),
                QuantityUnit = DoughQuantityUnit.FullLoads.ToString(),
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

        var loadCount = Math.Max(recommendation.RecommendedLoads, 1);
        var task = PrepTask.Create(
            recommendation.RecommendationDate,
            doughItem.Id,
            doughItem.PrepStationId,
            ApplicationRole.PizzaMaker,
            loadCount,
            recommendation.Id,
            PrepTaskType.MakeDoughLoad,
            DoughQuantityUnit.FullLoads,
            notes: BuildLoadTaskNote(loadCount, recommendation.MissingBalls));

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
            TaskType = task.TaskType.ToString(),
            QuantityUnit = task.QuantityUnit.ToString(),
            QuantityRecommended = task.QuantityRecommended,
            QuantityRecommendedBallsEquivalent = task.RecommendedBallsEquivalent,
            Status = task.Status.ToString(),
            Message = "Dough load task created. Completing it will create next-day ball dough work instead of counting balls available immediately."
        };
    }

    public async Task<SavePrepTaskResponse> CreateManualAsync(
        SavePrepTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prepItem = await GetValidatedPrepItemAsync(request.PrepItemId, request.PrepStationId, cancellationToken);
        var assignedRole = ParseAssignableRole(request.AssignedRole);
        var taskType = ParseTaskType(request.TaskType);
        var quantityUnit = ParseQuantityUnit(request.QuantityUnit);
        var storedQuantity = ResolvePlannedQuantity(taskType, quantityUnit, request.QuantityValue);

        var task = PrepTask.Create(
            request.TaskDate,
            prepItem.Id,
            prepItem.PrepStationId,
            assignedRole,
            storedQuantity,
            taskType: taskType,
            quantityUnit: taskType == PrepTaskType.MakeDoughLoad ? DoughQuantityUnit.FullLoads : DoughQuantityUnit.Balls,
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
        var taskType = ParseTaskType(request.TaskType);
        var quantityUnit = ParseQuantityUnit(request.QuantityUnit);
        var storedQuantity = ResolvePlannedQuantity(taskType, quantityUnit, request.QuantityValue);

        task.UpdateTask(
            request.TaskDate,
            prepItem.Id,
            prepItem.PrepStationId,
            assignedRole,
            taskType,
            taskType == PrepTaskType.MakeDoughLoad ? DoughQuantityUnit.FullLoads : DoughQuantityUnit.Balls,
            storedQuantity,
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

        if (request.QuantityValue <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.QuantityValue),
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

        var completionUnit = ParseQuantityUnit(request.QuantityUnit);
        var (storedCompletedQuantity, completedBallsEquivalent) = ResolveCompletionQuantity(task, completionUnit, request.QuantityValue);

        task.Complete(user.Id, storedCompletedQuantity, request.Notes);

        var message = "Prep task completed successfully.";

        switch (task.TaskType)
        {
            case PrepTaskType.MakeDoughLoad:
            {
                var createdBallTasks = await CreateBallDoughFollowUpAsync(task, storedCompletedQuantity, cancellationToken);
                await AppendProductionLedgerEntryAsync(
                    task.TaskDate,
                    MakeDoughLoadLedgerSourceType,
                    task.Id,
                    completedBallsEquivalent,
                    0,
                    0,
                    0,
                    request.Notes,
                    cancellationToken);
                message = createdBallTasks == 1
                    ? "Dough load completed. A ball dough task was created for tomorrow."
                    : $"Dough load completed. {createdBallTasks} ball dough tasks were created for tomorrow.";
                break;
            }

            case PrepTaskType.BallDough:
            {
                await ApplyBallDoughCompletionAsync(task, user.Id, completedBallsEquivalent, request.Notes, cancellationToken);
                await AppendProductionLedgerEntryAsync(
                    task.TaskDate,
                    BallDoughLedgerSourceType,
                    task.Id,
                    0,
                    completedBallsEquivalent,
                    0,
                    0,
                    request.Notes,
                    cancellationToken);
                message = "Ball dough completed. These balls now count as available inventory.";
                break;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompletePrepTaskResponse
        {
            PrepTaskId = task.Id,
            Status = task.Status.ToString(),
            TaskType = task.TaskType.ToString(),
            QuantityUnit = task.QuantityUnit.ToString(),
            QuantityCompleted = task.QuantityCompleted,
            QuantityCompletedBallsEquivalent = task.CompletedBallsEquivalent,
            CompletedAtUtc = task.CompletedAtUtc!.Value,
            Message = message
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

    private async Task<int> CreateBallDoughFollowUpAsync(
        PrepTask task,
        int completedLoadCount,
        CancellationToken cancellationToken)
    {
        var doughItem = task.PrepItem;
        if (doughItem is null)
        {
            throw new InvalidOperationException("The dough prep item must be available to create follow-up ball tasks.");
        }

        var ballingTaskDate = task.TaskDate.AddDays(1);
        var createdBallTasks = 0;

        for (var index = 0; index < completedLoadCount; index++)
        {
            var batch = new DoughBatch(
                Guid.NewGuid(),
                task.TaskDate,
                DoughBatch.StandardLoadCases,
                notes: BuildBatchNote(task, index + 1, completedLoadCount));

            await _doughBatchRepository.AddAsync(batch, cancellationToken);

            var ballTask = PrepTask.Create(
                ballingTaskDate,
                doughItem.Id,
                doughItem.PrepStationId,
                ApplicationRole.PizzaMaker,
                batch.TotalBalls,
                taskType: PrepTaskType.BallDough,
                quantityUnit: DoughQuantityUnit.Balls,
                sourcePrepTaskId: task.Id,
                sourceDoughBatchId: batch.Id,
                notes: BuildBallTaskNote(task.TaskDate, batch.TotalBalls));

            await _prepTaskRepository.AddAsync(ballTask, cancellationToken);
            createdBallTasks++;
        }

        return createdBallTasks;
    }

    private async Task ApplyBallDoughCompletionAsync(
        PrepTask task,
        string actingUserId,
        int actualBallsCreated,
        string? notes,
        CancellationToken cancellationToken)
    {
        DoughBatch? sourceBatch = null;
        DateOnly sourceDate = task.TaskDate;

        if (task.SourceDoughBatchId.HasValue)
        {
            sourceBatch = await _doughBatchRepository.GetByIdAsync(task.SourceDoughBatchId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("The dough batch linked to this ball dough task could not be found.");

            if (actualBallsCreated > sourceBatch.TotalBalls)
            {
                throw new InvalidOperationException("Actual balls created cannot be greater than the source dough load capacity.");
            }

            if (sourceBatch.IsBalled)
            {
                throw new InvalidOperationException("This dough batch has already been marked as balled.");
            }

            sourceBatch.MarkAsBalled(task.CompletedAtUtc ?? DateTime.UtcNow);
            sourceDate = sourceBatch.BatchDate;
        }

        await UpsertInventorySnapshotAsync(task.TaskDate, actualBallsCreated, cancellationToken);

        var qualityRecord = DoughBatchQualityRecord.Create(
            sourceDate,
            task.CompletedAtUtc ?? DateTime.UtcNow,
            actualBallsCreated,
            actingUserId,
            task.Id,
            DoughQualityStatus.Good,
            managerNote: notes);

        await _doughBatchQualityRepository.AddAsync(qualityRecord, cancellationToken);
    }

    private async Task AppendProductionLedgerEntryAsync(
        DateOnly occurredOn,
        string sourceType,
        Guid sourceEntityId,
        int totalBallsCreated,
        int ballsCompleted,
        int ballsReballed,
        int ballsDiscarded,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (_productionLedgerRepository is null)
        {
            return;
        }

        await _productionLedgerRepository.AddAsync(
            new ProductionLedger(
                Guid.NewGuid(),
                occurredOn,
                sourceType,
                sourceEntityId,
                totalBallsCreated,
                ballsCompleted,
                ballsReballed,
                ballsDiscarded,
                notes),
            cancellationToken);
    }

    private async Task UpsertInventorySnapshotAsync(
        DateOnly snapshotDate,
        int ballsToAdd,
        CancellationToken cancellationToken)
    {
        var latestSnapshot = await _doughInventorySnapshotRepository.GetLatestOnOrBeforeForUpdateAsync(snapshotDate, cancellationToken);

        if (latestSnapshot is not null && latestSnapshot.SnapshotDate == snapshotDate)
        {
            latestSnapshot.UpdateSnapshot(
                snapshotDate,
                latestSnapshot.AvailableBalls + ballsToAdd,
                latestSnapshot.NewBalls + ballsToAdd,
                latestSnapshot.OldBalls,
                latestSnapshot.ReservedBalls,
                latestSnapshot.UsedBalls,
                latestSnapshot.WasteBalls,
                latestSnapshot.Notes);

            return;
        }

        var priorAvailableBalls = latestSnapshot?.AvailableBalls ?? 0;
        var snapshot = new DoughInventorySnapshot(
            Guid.NewGuid(),
            snapshotDate,
            priorAvailableBalls + ballsToAdd,
            ballsToAdd,
            priorAvailableBalls,
            latestSnapshot?.ReservedBalls ?? 0,
            0,
            latestSnapshot?.WasteBalls ?? 0,
            "Created from Ball Dough completion.");

        await _doughInventorySnapshotRepository.AddAsync(snapshot, cancellationToken);
    }

    private static (int StoredCompletedQuantity, int CompletedBallsEquivalent) ResolveCompletionQuantity(
        PrepTask task,
        DoughQuantityUnit requestedUnit,
        int quantityValue)
    {
        return task.TaskType switch
        {
            PrepTaskType.MakeDoughLoad => requestedUnit == DoughQuantityUnit.FullLoads
                ? (quantityValue, quantityValue * DoughRules.StandardBatchBalls)
                : throw new InvalidOperationException("Make dough load tasks must be completed in full loads."),

            PrepTaskType.BallDough => (DoughRules.ConvertToBalls(quantityValue, requestedUnit), DoughRules.ConvertToBalls(quantityValue, requestedUnit)),

            _ => (DoughRules.ConvertToBalls(quantityValue, requestedUnit), DoughRules.ConvertToBalls(quantityValue, requestedUnit))
        };
    }

    private static int ResolvePlannedQuantity(PrepTaskType taskType, DoughQuantityUnit requestedUnit, int quantityValue)
    {
        if (quantityValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityValue), "Quantity must be greater than zero.");
        }

        return taskType switch
        {
            PrepTaskType.MakeDoughLoad when requestedUnit != DoughQuantityUnit.FullLoads
                => throw new InvalidOperationException("Make dough load tasks must be planned in full loads."),
            PrepTaskType.MakeDoughLoad
                => quantityValue,
            PrepTaskType.BallDough
                => DoughRules.ConvertToBalls(quantityValue, requestedUnit),
            _ => DoughRules.ConvertToBalls(quantityValue, requestedUnit)
        };
    }

    private static string BuildLoadTaskNote(int loadCount, int missingBalls)
    {
        var potentialBalls = loadCount * DoughRules.StandardBatchBalls;
        return $"We need this dough load today so it can be balled tomorrow. Potential: {potentialBalls} balls tomorrow. Original shortage: {missingBalls} balls.";
    }

    private static string BuildBatchNote(PrepTask sourceTask, int loadNumber, int totalLoads)
    {
        return totalLoads == 1
            ? $"Created from Make Dough Load task {sourceTask.Id}."
            : $"Created from Make Dough Load task {sourceTask.Id}, load {loadNumber} of {totalLoads}.";
    }

    private static string BuildBallTaskNote(DateOnly sourceLoadDate, int expectedBalls)
    {
        return $"This dough load from {sourceLoadDate:ddd, MMM d} is ready to be balled today. These balls count as available only after this task is completed. Expected: {expectedBalls} balls.";
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
            TaskType = task.TaskType.ToString(),
            QuantityUnit = task.QuantityUnit.ToString(),
            QuantityRecommended = task.QuantityRecommended,
            QuantityRecommendedBallsEquivalent = task.RecommendedBallsEquivalent,
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

    private static PrepTaskType ParseTaskType(string? taskType)
    {
        var candidate = string.IsNullOrWhiteSpace(taskType)
            ? nameof(PrepTaskType.GenericDough)
            : taskType;

        if (!PrepTaskTypeExtensions.TryParse(candidate, out var parsedTaskType))
        {
            throw new ArgumentException("Task type is not valid for prep tasks.", nameof(taskType));
        }

        return parsedTaskType;
    }

    private static DoughQuantityUnit ParseQuantityUnit(string quantityUnit)
    {
        if (!Enum.TryParse<DoughQuantityUnit>(quantityUnit, true, out var parsedQuantityUnit))
        {
            throw new ArgumentException("Quantity unit is not valid for dough tasks.", nameof(quantityUnit));
        }

        return parsedQuantityUnit;
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
            TaskType = task.TaskType.ToString(),
            QuantityUnit = task.QuantityUnit.ToString(),
            QuantityRecommended = task.QuantityRecommended,
            QuantityRecommendedBallsEquivalent = task.RecommendedBallsEquivalent,
            Status = task.Status.ToString(),
            Message = message
        };
    }
}
