using System.Text.Json;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Constants;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.OperationalSimulation;

public sealed class OperationalSimulationService : IOperationalSimulationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly AsyncLocal<bool> AuditPersistenceSuppressed = new();
    private const string WeeklyCorrectionDraftType = "WeeklyCorrection";
    private const string WeeklyClosingPreviewDraftType = "WeeklyClosingPreview";
    private const string DoughTaskDraftType = "DoughTask";
    private const string DailyClosingDraftType = "DailyClosing";
    private const string RestaurantEventDraftType = "RestaurantEvent";
    private const string ProjectionAdjustmentDraftType = "ProjectionAdjustment";

    private readonly IDailyDoughClosingReadService _dailyDoughClosingReadService;
    private readonly IDoughAvailabilityProjectionService _doughAvailabilityProjectionService;
    private readonly IDoughBatchReadRepository _doughBatchReadRepository;
    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;
    private readonly IOperationalAuditEntryRepository _operationalAuditEntryRepository;
    private readonly IOperationalDraftRepository _operationalDraftRepository;
    private readonly IOperationalIntentClassifier _operationalIntentClassifier;
    private readonly IOperationalProjectionService? _operationalProjectionService;
    private readonly IPrepItemReadRepository _prepItemReadRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;
    private readonly IRestaurantEventManagementService _restaurantEventManagementService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public OperationalSimulationService(
        IDailyDoughClosingReadService dailyDoughClosingReadService,
        IDoughAvailabilityProjectionService doughAvailabilityProjectionService,
        IDoughBatchReadRepository doughBatchReadRepository,
        IDoughInventoryImpactReadService doughInventoryImpactReadService,
        IOperationalAuditEntryRepository operationalAuditEntryRepository,
        IOperationalDraftRepository operationalDraftRepository,
        IOperationalIntentClassifier operationalIntentClassifier,
        IOperationalProjectionService? operationalProjectionService,
        IPrepItemReadRepository prepItemReadRepository,
        IPrepTaskRepository prepTaskRepository,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService,
        IRestaurantEventManagementService restaurantEventManagementService,
        IUnitOfWork unitOfWork,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _dailyDoughClosingReadService = dailyDoughClosingReadService;
        _doughAvailabilityProjectionService = doughAvailabilityProjectionService;
        _doughBatchReadRepository = doughBatchReadRepository;
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
        _operationalAuditEntryRepository = operationalAuditEntryRepository;
        _operationalDraftRepository = operationalDraftRepository;
        _operationalIntentClassifier = operationalIntentClassifier;
        _operationalProjectionService = operationalProjectionService;
        _prepItemReadRepository = prepItemReadRepository;
        _prepTaskRepository = prepTaskRepository;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
        _restaurantEventManagementService = restaurantEventManagementService;
        _unitOfWork = unitOfWork;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
    }

    public async Task<OperationalSimulationResult> SimulateAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceText);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var actorUserId = ResolveActorUserId(request.ActorUserId);
        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1
            ? 8
            : request.HistoricalWeeksToUse;
        var targetWeekStartDate = NormalizeClosingWeekStart(request.TargetWeekStartDate ?? request.ReferenceDate);
        var intent = await _operationalIntentClassifier.ClassifyAsync(
            request.SourceText,
            request.ReferenceDate,
            request.TargetWeekStartDate,
            cancellationToken);
        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = request.ReferenceDate
            },
            cancellationToken);
        var availability = await _doughAvailabilityProjectionService.GetWeeklyAvailabilityAsync(
            request.ReferenceDate,
            cancellationToken);
        var weeklyGoal = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            request.ReferenceDate,
            historicalWeeksToUse,
            cancellationToken);
        var inventoryImpact = await _doughInventoryImpactReadService.GetInventoryImpactAsync(
            new GetDoughInventoryImpactRequest
            {
                ReferenceDate = request.ReferenceDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);
        var existingClosing = await FindClosingForWeekAsync(targetWeekStartDate, cancellationToken);
        var existingDraft = await _operationalDraftRepository.GetLatestByCorrelationIdAsync(correlationId, cancellationToken);
        var doughItem = await _prepItemReadRepository.GetByCodeAsync(PrepCatalogCodes.DoughItem, cancellationToken);
        var tasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            targetWeekStartDate,
            request.ReferenceDate.AddDays(1),
            cancellationToken);
        var batches = await _doughBatchReadRepository.SearchForCorrectionAsync(
            targetWeekStartDate,
            request.ReferenceDate.AddDays(1),
            includeVoided: true,
            cancellationToken);
        var warnings = new List<OperationalValidationWarning>();

        var (beforeSnapshot, afterPreview, diffEntries, weeklyCorrectionProposal, doughTaskDraftProposal) =
            BuildPreview(
                intent,
                request.ReferenceDate,
                existingClosing,
                existingDraft,
                carryover,
                availability,
                weeklyGoal,
                inventoryImpact,
                doughItem,
                warnings);

        ApplyExistingWeeklyClosingWarnings(existingClosing, warnings);
        ApplyExistingDraftWarnings(existingDraft, warnings);
        ApplyDuplicateLoadPreventionWarnings(weeklyCorrectionProposal, doughTaskDraftProposal, weeklyGoal, tasks, batches, warnings);
        ApplyCarryoverConsistencyWarnings(weeklyCorrectionProposal, availability, weeklyGoal, warnings);
        ApplyWeeklyClosingConsistencyWarnings(weeklyCorrectionProposal, availability, weeklyGoal, warnings);
        ApplyMissingCatalogWarnings(doughItem, doughTaskDraftProposal, warnings);

        var simulation = new OperationalSimulationResult
        {
            CorrelationId = correlationId,
            SourceText = request.SourceText.Trim(),
            Intent = intent,
            BeforeSnapshotJson = Serialize(beforeSnapshot),
            AfterPreviewJson = Serialize(afterPreview),
            DiffJson = Serialize(diffEntries),
            ValidationWarningsJson = Serialize(warnings),
            ValidationWarnings = warnings,
            WeeklyCorrectionProposal = weeklyCorrectionProposal,
            DoughTaskDraftProposal = doughTaskDraftProposal,
            ExistingWeeklyClosing = existingClosing,
            Carryover = carryover,
            Availability = availability,
            WeeklyGoal = weeklyGoal,
            InventoryImpact = inventoryImpact
        };

        if (!AuditPersistenceSuppressed.Value)
        {
            var auditEntry = OperationalAuditEntry.Create(
                correlationId,
                "SimulateOperationalNarrative",
                actorUserId,
                simulation.SourceText,
                SerializeIntent(intent),
                simulation.BeforeSnapshotJson,
                simulation.AfterPreviewJson,
                simulation.ValidationWarningsJson,
                existingDraft?.Id);

            await _operationalAuditEntryRepository.AddAsync(auditEntry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return simulation;
    }

    public async Task<OperationalSimulationResult> SimulateDoughTaskAsync(
        OperationalDoughTaskDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var actorUserId = ResolveActorUserId(request.ActorUserId);
        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1
            ? 8
            : request.HistoricalWeeksToUse;
        var doughItem = await _prepItemReadRepository.GetByCodeAsync(PrepCatalogCodes.DoughItem, cancellationToken);
        var weeklyGoal = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            request.TaskDate,
            historicalWeeksToUse,
            cancellationToken);
        var availability = await _doughAvailabilityProjectionService.GetWeeklyAvailabilityAsync(
            request.TaskDate,
            cancellationToken);
        var inventoryImpact = await _doughInventoryImpactReadService.GetInventoryImpactAsync(
            new GetDoughInventoryImpactRequest
            {
                ReferenceDate = request.TaskDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);
        var tasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            request.TaskDate,
            request.TaskDate,
            cancellationToken);
        var batches = await _doughBatchReadRepository.SearchForCorrectionAsync(
            request.TaskDate.AddDays(-1),
            request.TaskDate,
            includeVoided: true,
            cancellationToken);
        var existingDrafts = await _operationalDraftRepository.ListByCorrelationIdAsync(correlationId, cancellationToken);
        var warnings = new List<OperationalValidationWarning>();
        var normalizedTaskType = NormalizeTaskType(request.TaskType);
        var reusableExistingTask = ResolveReusableExistingTask(request, normalizedTaskType, tasks, warnings);

        var doughTaskDraftProposal = new DoughTaskDraftProposal
        {
            ExistingPrepTaskId = reusableExistingTask?.Id ?? request.ExistingPrepTaskId,
            TaskDate = request.TaskDate,
            TaskType = normalizedTaskType,
            Quantity = request.QuantityValue,
            QuantityUnit = NormalizeQuantityUnit(request.QuantityUnit),
            AssignedRole = NormalizeAssignedRole(request.AssignedRole),
            PrepItemId = doughItem?.Id ?? Guid.Empty,
            PrepStationId = doughItem?.PrepStationId ?? Guid.Empty,
            Notes = request.Notes ?? "Structured operational dough task draft.",
            AutoCompleteOnApproval = request.AutoCompleteOnApproval,
            CompletionQuantity = request.CompletionQuantityValue
        };
        var intent = new ProductionIntent(
            BuildDoughTaskSourceText(doughTaskDraftProposal),
            $"Structured production draft for {doughTaskDraftProposal.TaskType} on {doughTaskDraftProposal.TaskDate:yyyy-MM-dd}.",
            request.TaskDate,
            doughTaskDraftProposal.TaskType == nameof(PrepTaskType.MakeDoughLoad),
            doughTaskDraftProposal.TaskType == nameof(PrepTaskType.BallDough),
            doughTaskDraftProposal.Quantity,
            doughTaskDraftProposal.Notes);

        ApplyDuplicateLoadPreventionWarnings(null, doughTaskDraftProposal, weeklyGoal, tasks, batches, warnings);
        ApplyMissingCatalogWarnings(doughItem, doughTaskDraftProposal, warnings);
        ApplyExistingStructuredDraftWarnings(
            existingDrafts,
            DoughTaskDraftType,
            draft => draft.DraftPayloadJson.Contains($"\"taskDate\":\"{request.TaskDate:yyyy-MM-dd}\"", StringComparison.OrdinalIgnoreCase) &&
                     draft.DraftPayloadJson.Contains($"\"taskType\":\"{doughTaskDraftProposal.TaskType}\"", StringComparison.OrdinalIgnoreCase),
            "A dough task draft already exists for the same date and task type within this operational slice.",
            warnings);

        if (reusableExistingTask is not null)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "existing-task-will-be-completed",
                Message = "A matching open prep task already exists, so approval will complete that task instead of creating a duplicate.",
                RequiresHumanReview = false
            });
        }

        if (string.Equals(doughTaskDraftProposal.TaskType, nameof(PrepTaskType.MakeDoughLoad), StringComparison.Ordinal))
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "make-load-not-ready-now",
                Message = "Make Dough Load drafts do not increase ReadyNow inventory until a BallDough completion is recorded.",
                RequiresHumanReview = false
            });
        }

        var beforeSnapshot = new
        {
            request.TaskDate,
            ExistingPrepTaskId = reusableExistingTask?.Id ?? request.ExistingPrepTaskId,
            weeklyGoal.ReadyNowBalls,
            weeklyGoal.MixedButNotBalledLoads,
            weeklyGoal.StillMissingThisWeekBalls,
            ExistingTaskCount = tasks.Count
        };
        var afterPreview = new
        {
            doughTaskDraftProposal,
            ReadyNowImpactBalls = string.Equals(doughTaskDraftProposal.TaskType, nameof(PrepTaskType.BallDough), StringComparison.Ordinal)
                ? doughTaskDraftProposal.Quantity
                : 0
        };
        var diffEntries = new object[]
        {
            new
            {
                Field = "TaskBackfill",
                Before = "Not drafted",
                After = $"{doughTaskDraftProposal.TaskType} on {doughTaskDraftProposal.TaskDate:yyyy-MM-dd}"
            }
        };

        var simulation = new OperationalSimulationResult
        {
            CorrelationId = correlationId,
            SourceText = intent.SourceText,
            Intent = intent,
            BeforeSnapshotJson = Serialize(beforeSnapshot),
            AfterPreviewJson = Serialize(afterPreview),
            DiffJson = Serialize(diffEntries),
            ValidationWarningsJson = Serialize(warnings),
            ValidationWarnings = warnings,
            DoughTaskDraftProposal = doughTaskDraftProposal,
            Availability = availability,
            WeeklyGoal = weeklyGoal,
            InventoryImpact = inventoryImpact
        };

        await PersistSimulationAuditAsync(simulation, actorUserId, cancellationToken);
        return simulation;
    }

    public async Task<OperationalSimulationResult> SimulateDailyClosingAsync(
        OperationalDailyClosingDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var actorUserId = ResolveActorUserId(request.ActorUserId);
        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1
            ? 8
            : request.HistoricalWeeksToUse;
        var weekSummary = await _dailyDoughClosingReadService.GetWeekSummaryAsync(
            new GetDailyClosingWeekSummaryRequest
            {
                ReferenceDate = request.ClosingDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);
        var daySummary = weekSummary.Days.FirstOrDefault(day => day.Date == request.ClosingDate);
        var existingDrafts = await _operationalDraftRepository.ListByCorrelationIdAsync(correlationId, cancellationToken);
        var warnings = new List<OperationalValidationWarning>();

        ValidateUsageBreakdown(request.ActualUsedBalls, request.UsageBreakdown, warnings);
        ApplyExistingStructuredDraftWarnings(
            existingDrafts,
            DailyClosingDraftType,
            draft => draft.DraftPayloadJson.Contains($"\"closingDate\":\"{request.ClosingDate:yyyy-MM-dd}\"", StringComparison.OrdinalIgnoreCase),
            "A daily closing draft already exists for the same operational date within this slice.",
            warnings);

        if (daySummary?.DailyClosingId is not null)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "existing-daily-closing",
                Message = "A daily closing already exists for this date, so the draft will need human review as a correction.",
                RequiresHumanReview = true
            });
        }

        var eventUsageBalls = request.UsageBreakdown
            .Where(component => string.Equals(component.Category, "Event", StringComparison.OrdinalIgnoreCase))
            .Sum(component => component.Balls);

        if (eventUsageBalls > 0)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "event-usage-embedded-in-daily-closing",
                Message = "This daily closing already includes event usage. Weekly previews must not add the event balls a second time.",
                RequiresHumanReview = false
            });
        }

        var proposal = new DailyClosingDraftProposal
        {
            ExistingDailyClosingId = daySummary?.DailyClosingId,
            ClosingDate = request.ClosingDate,
            ForecastNeededBalls = daySummary?.ForecastNeededBalls ?? 0,
            ActualUsedBalls = request.ActualUsedBalls,
            UsageBreakdown = request.UsageBreakdown,
            Notes = request.Notes ?? BuildDailyClosingNotes(request.UsageBreakdown),
            CorrectionNote = daySummary?.DailyClosingId is null
                ? null
                : "Operational slice preview correction."
        };
        var intent = new ConsumptionIntent(
            BuildDailyClosingSourceText(request.ClosingDate, request.ActualUsedBalls, request.UsageBreakdown),
            $"Structured daily closing draft for {request.ClosingDate:yyyy-MM-dd}.",
            request.ClosingDate,
            false,
            request.ActualUsedBalls,
            proposal.Notes);

        var beforeSnapshot = new
        {
            request.ClosingDate,
            ExistingDailyClosingId = daySummary?.DailyClosingId,
            ExistingForecastNeededBalls = daySummary?.ForecastNeededBalls,
            ExistingActualUsedBalls = daySummary?.ActualUsedBalls,
            ExistingNotes = daySummary?.Notes,
            weekSummary.TotalActualUsedBalls
        };
        var afterPreview = new
        {
            proposal
        };
        var diffEntries = new object[]
        {
            new
            {
                Field = "ActualUsedBalls",
                Before = daySummary?.ActualUsedBalls,
                After = proposal.ActualUsedBalls
            },
            new
            {
                Field = "DailyVariance",
                Before = daySummary?.DailyVariance,
                After = proposal.ForecastNeededBalls - proposal.ActualUsedBalls
            }
        };

        var simulation = new OperationalSimulationResult
        {
            CorrelationId = correlationId,
            SourceText = intent.SourceText,
            Intent = intent,
            BeforeSnapshotJson = Serialize(beforeSnapshot),
            AfterPreviewJson = Serialize(afterPreview),
            DiffJson = Serialize(diffEntries),
            ValidationWarningsJson = Serialize(warnings),
            ValidationWarnings = warnings,
            DailyClosingDraftProposal = proposal
        };

        await PersistSimulationAuditAsync(simulation, actorUserId, cancellationToken);
        return simulation;
    }

    public async Task<OperationalSimulationResult> SimulateRestaurantEventAsync(
        OperationalEventDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var actorUserId = ResolveActorUserId(request.ActorUserId);
        var existingEvents = await _restaurantEventManagementService.SearchAsync(
            request.EventDate,
            request.EventDate,
            request.Name,
            activeOnly: false,
            cancellationToken);
        var existingEvent = existingEvents.FirstOrDefault(item =>
            string.Equals(item.Name, request.Name, StringComparison.OrdinalIgnoreCase));
        var existingDrafts = await _operationalDraftRepository.ListByCorrelationIdAsync(correlationId, cancellationToken);
        var warnings = new List<OperationalValidationWarning>();

        if (request.ExpectedPeopleMinimum <= 0 || request.ExpectedPeopleMaximum <= 0)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "invalid-people-range",
                Message = "Event people range must be greater than zero.",
                BlocksDraft = true
            });
        }
        else if (request.ExpectedPeopleMaximum < request.ExpectedPeopleMinimum)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "reversed-people-range",
                Message = "Event people range is reversed and must be corrected before drafting.",
                BlocksDraft = true
            });
        }

        ApplyExistingStructuredDraftWarnings(
            existingDrafts,
            RestaurantEventDraftType,
            draft => draft.DraftPayloadJson.Contains($"\"eventDate\":\"{request.EventDate:yyyy-MM-dd}\"", StringComparison.OrdinalIgnoreCase) &&
                     draft.DraftPayloadJson.Contains($"\"name\":\"{request.Name}\"", StringComparison.OrdinalIgnoreCase),
            "A restaurant event draft already exists for the same date and event name within this slice.",
            warnings);

        if (request.PreviousNarrativeDoughBalls.HasValue &&
            request.PreviousNarrativeDoughBalls.Value != request.EstimatedDoughBalls)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "event-usage-narrative-mismatch",
                Message = $"Previous narrative said {request.PreviousNarrativeDoughBalls.Value} balls for this event, but the current slice says {request.EstimatedDoughBalls}. Human confirmation is required before approval.",
                RequiresHumanReview = true
            });
        }

        if (existingEvent is not null && existingEvent.EstimatedDoughBalls != request.EstimatedDoughBalls)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "existing-event-balls-mismatch",
                Message = "An existing restaurant event record has a different dough ball estimate than the current draft.",
                RequiresHumanReview = true
            });
        }

        var proposal = new RestaurantEventDraftProposal
        {
            ExistingRestaurantEventId = existingEvent?.Id,
            EventDate = request.EventDate,
            Name = request.Name,
            EstimatedPizzas = request.EstimatedDoughBalls,
            EstimatedDoughBalls = request.EstimatedDoughBalls,
            ExpectedPeopleMinimum = request.ExpectedPeopleMinimum,
            ExpectedPeopleMaximum = request.ExpectedPeopleMaximum,
            AllowShortFermentation = request.AllowShortFermentation,
            PreviousNarrativeDoughBalls = request.PreviousNarrativeDoughBalls,
            Notes = request.Notes ?? $"Attendance range: {request.ExpectedPeopleMinimum}-{request.ExpectedPeopleMaximum} people."
        };
        var intent = new SalesIntent(
            BuildRestaurantEventSourceText(request),
            $"Structured external event draft for {request.Name} on {request.EventDate:yyyy-MM-dd}.",
            request.EventDate,
            "ExternalEvent",
            proposal.EstimatedPizzas);

        var beforeSnapshot = new
        {
            request.EventDate,
            ExistingRestaurantEventId = existingEvent?.Id,
            ExistingEstimatedDoughBalls = existingEvent?.EstimatedDoughBalls,
            ExistingNotes = existingEvent?.Notes,
            request.PreviousNarrativeDoughBalls
        };
        var afterPreview = new
        {
            proposal
        };
        var diffEntries = new object[]
        {
            new
            {
                Field = "EstimatedDoughBalls",
                Before = existingEvent?.EstimatedDoughBalls,
                After = proposal.EstimatedDoughBalls
            },
            new
            {
                Field = "AttendanceRange",
                Before = existingEvent is null ? null : $"{existingEvent.EstimatedPizzas} pizzas estimated",
                After = $"{proposal.ExpectedPeopleMinimum}-{proposal.ExpectedPeopleMaximum} people / {proposal.EstimatedPizzas} pizzas"
            }
        };

        var simulation = new OperationalSimulationResult
        {
            CorrelationId = correlationId,
            SourceText = intent.SourceText,
            Intent = intent,
            BeforeSnapshotJson = Serialize(beforeSnapshot),
            AfterPreviewJson = Serialize(afterPreview),
            DiffJson = Serialize(diffEntries),
            ValidationWarningsJson = Serialize(warnings),
            ValidationWarnings = warnings,
            RestaurantEventDraftProposal = proposal
        };

        await PersistSimulationAuditAsync(simulation, actorUserId, cancellationToken);
        return simulation;
    }

    public async Task<OperationalSimulationResult> SimulateOperationalProjectionAsync(
        OperationalProjectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_operationalProjectionService is null)
        {
            throw new InvalidOperationException("Operational projection is not configured for this simulation service instance.");
        }

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var actorUserId = ResolveActorUserId(request.ActorUserId);
        var projection = await _operationalProjectionService.ProjectAsync(
            request with { CorrelationId = correlationId },
            cancellationToken);
        var warnings = projection.ValidationWarnings.ToArray();
        var suggestedAdditionalBallDoughBalls = projection.ProjectedShortageBalls;
        var suggestedAdditionalMakeDoughLoads = projection.ProjectedShortageBalls <= 0
            ? 0
            : (int)Math.Ceiling(projection.ProjectedShortageBalls / (double)DoughRules.StandardBatchBalls);
        var proposal = new ProjectionAdjustmentDraftProposal
        {
            ReferenceDate = projection.ReferenceDate,
            WeekStartDate = projection.WeekStartDate,
            WeekEndDate = projection.WeekEndDate,
            ReadyNowBalls = projection.ReadyNowBalls,
            BallsReadyForService = projection.BallsReadyForService,
            RemainingWeekDemandBalls = projection.RemainingWeekDemandBalls,
            ProjectedCoverageBalls = projection.ProjectedCoverageBalls,
            ProjectedShortageBalls = projection.ProjectedShortageBalls,
            SuggestedAdditionalBallDoughBalls = suggestedAdditionalBallDoughBalls,
            SuggestedAdditionalMakeDoughLoads = suggestedAdditionalMakeDoughLoads,
            Notes = request.Notes ?? "Projection-based planning adjustment draft only. Human approval is required before any operational change."
        };
        var intent = new InventoryIntent(
            BuildProjectionSourceText(projection),
            $"Projection view for {projection.ReferenceDate:yyyy-MM-dd}: {projection.BallsReadyForService} balls ready for service with {projection.ProjectedShortageBalls} projected shortage.",
            projection.ReferenceDate,
            projection.BallsReadyForService,
            projection.MixedButNotBalledBalls <= 0
                ? 0
                : (int)Math.Ceiling(projection.MixedButNotBalledBalls / (double)DoughRules.StandardBatchBalls),
            proposal.Notes);
        var beforeSnapshot = new
        {
            projection.ReferenceDate,
            projection.ReadyNowBalls,
            projection.FutureBalls,
            projection.RemainingWeekDemandBalls,
            projection.WeeklyClosingUsageConsistent
        };
        var afterPreview = new
        {
            projection,
            proposal
        };
        var projectedShortageWithoutFuture = Math.Max(projection.RemainingWeekDemandBalls - projection.ReadyNowBalls, 0);
        var diffEntries = new object[]
        {
            new
            {
                Field = "BallsReadyForService",
                Before = projection.ReadyNowBalls,
                After = projection.BallsReadyForService
            },
            new
            {
                Field = "ProjectedShortageBalls",
                Before = projectedShortageWithoutFuture,
                After = projection.ProjectedShortageBalls
            },
            new
            {
                Field = "ProjectionLayer",
                Before = "Operational truth only",
                After = "Operational truth plus planning-safe projection"
            }
        };

        var simulation = new OperationalSimulationResult
        {
            CorrelationId = correlationId,
            SourceText = intent.SourceText,
            Intent = intent,
            BeforeSnapshotJson = Serialize(beforeSnapshot),
            AfterPreviewJson = Serialize(afterPreview),
            DiffJson = Serialize(diffEntries),
            ValidationWarningsJson = Serialize(warnings),
            ValidationWarnings = warnings,
            OperationalProjection = projection,
            ProjectionAdjustmentDraftProposal = proposal
        };

        await PersistSimulationAuditAsync(simulation, actorUserId, cancellationToken);
        return simulation;
    }

    public async Task<OperationalSimulationResult> SimulateWeeklyClosingPreviewAsync(
        OperationalWeeklyClosingPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var actorUserId = ResolveActorUserId(request.ActorUserId);
        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1
            ? 8
            : request.HistoricalWeeksToUse;
        var dailyClosingWeekSummary = await _dailyDoughClosingReadService.GetWeekSummaryAsync(
            new GetDailyClosingWeekSummaryRequest
            {
                ReferenceDate = request.ReferenceDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);
        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = request.ReferenceDate
            },
            cancellationToken);
        var availability = await _doughAvailabilityProjectionService.GetWeeklyAvailabilityAsync(
            request.ReferenceDate,
            cancellationToken);
        var weeklyGoal = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            request.ReferenceDate,
            historicalWeeksToUse,
            cancellationToken);
        var inventoryImpact = await _doughInventoryImpactReadService.GetInventoryImpactAsync(
            new GetDoughInventoryImpactRequest
            {
                ReferenceDate = request.ReferenceDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);
        var closingWeekStartDate = NormalizeClosingWeekStart(request.WeekStartDate);
        var existingClosing = await FindClosingForWeekAsync(closingWeekStartDate, cancellationToken);
        var persistedDrafts = await _operationalDraftRepository.ListByCorrelationIdAsync(correlationId, cancellationToken);
        var dailyClosingDrafts = persistedDrafts
            .Where(draft => string.Equals(draft.DraftType, DailyClosingDraftType, StringComparison.Ordinal))
            .Select(draft => DeserializePayload<DailyClosingApprovalPayload>(draft.DraftPayloadJson))
            .Where(payload => payload.ClosingDate >= request.WeekStartDate && payload.ClosingDate <= request.ReferenceDate)
            .ToArray();
        var eventDrafts = persistedDrafts
            .Where(draft => string.Equals(draft.DraftType, RestaurantEventDraftType, StringComparison.Ordinal))
            .Select(draft => DeserializePayload<RestaurantEventApprovalPayload>(draft.DraftPayloadJson))
            .Where(payload => payload.EventDate >= request.WeekStartDate && payload.EventDate <= request.ReferenceDate)
            .ToArray();
        var productionDrafts = persistedDrafts
            .Where(draft => string.Equals(draft.DraftType, DoughTaskDraftType, StringComparison.Ordinal))
            .Select(draft => DeserializePayload<DoughTaskApprovalPayload>(draft.DraftPayloadJson))
            .Where(payload => payload.TaskDate >= request.WeekStartDate && payload.TaskDate <= request.ReferenceDate)
            .ToArray();
        var warnings = new List<OperationalValidationWarning>();
        var draftDailyClosingTotals = dailyClosingDrafts.Sum(payload => payload.ActualUsedBalls);
        var usedBallsFromSlice = draftDailyClosingTotals > 0
            ? draftDailyClosingTotals
            : dailyClosingWeekSummary.TotalActualUsedBalls;

        ApplyExistingWeeklyClosingWarnings(existingClosing, warnings);

        if (draftDailyClosingTotals > 0 && usedBallsFromSlice != weeklyGoal.ActualUsedBallsThisWeek)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "weekly-used-balls-overlay",
                Message = $"The weekly preview is using {usedBallsFromSlice} balls from persisted daily closing drafts instead of the live task-derived total {weeklyGoal.ActualUsedBallsThisWeek}. Review before approval.",
                RequiresHumanReview = true
            });
        }

        if (productionDrafts.Length > 0)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "production-drafts-excluded-from-usage",
                Message = "Production drafts are intentionally excluded from UsedBalls. Weekly usage is driven by daily closing drafts only.",
                RequiresHumanReview = false
            });
        }

        if (productionDrafts.Any(draft => string.Equals(draft.TaskType, nameof(PrepTaskType.MakeDoughLoad), StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "make-load-pending-not-ready-now",
                Message = "Pending Make Dough Load drafts do not increase ReadyNow carryover until their BallDough follow-up is completed.",
                RequiresHumanReview = false
            });
        }

        foreach (var eventDraft in eventDrafts)
        {
            if (eventDraft.PreviousNarrativeDoughBalls.HasValue &&
                eventDraft.PreviousNarrativeDoughBalls.Value != eventDraft.EstimatedDoughBalls)
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "event-usage-narrative-mismatch",
                    Message = $"Previous narrative said {eventDraft.PreviousNarrativeDoughBalls.Value} balls for event '{eventDraft.Name}', but the current slice says {eventDraft.EstimatedDoughBalls}. Human confirmation is required before approval.",
                    RequiresHumanReview = true
                });
            }

            var matchingDailyEventBalls = dailyClosingDrafts
                .Where(draft => draft.ClosingDate == eventDraft.EventDate)
                .SelectMany(draft => draft.UsageBreakdown)
                .Where(component =>
                    string.Equals(component.Category, "Event", StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(component.ReferenceName) ||
                     string.Equals(component.ReferenceName, eventDraft.Name, StringComparison.OrdinalIgnoreCase)))
                .Sum(component => component.Balls);

            if (matchingDailyEventBalls == eventDraft.EstimatedDoughBalls)
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "event-usage-already-counted-in-daily-closing",
                    Message = $"Event '{eventDraft.Name}' is already embedded in the daily closing total for {eventDraft.EventDate:yyyy-MM-dd}; weekly UsedBalls will not add it twice.",
                    RequiresHumanReview = false
                });
            }
            else
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "event-daily-closing-reconciliation",
                    Message = $"Event '{eventDraft.Name}' does not reconcile cleanly against the same-day daily closing breakdown. Review before approval.",
                    RequiresHumanReview = true
                });
            }
        }

        var weeklyCorrectionProposal = new WeeklyCorrectionProposal
        {
            ExistingWeeklyClosingId = existingClosing?.Id,
            WeekStartDate = closingWeekStartDate,
            NeededBalls = existingClosing?.NeededBalls ?? weeklyGoal.WeekTotalNeededBalls,
            ProducedBalls = existingClosing?.ProducedBalls ?? availability.ProducedThisWeekBalls,
            UsedBalls = usedBallsFromSlice,
            LostBalls = existingClosing?.LostBalls ?? availability.LostBallsThisWeek,
            LeftoverReadyBalls = existingClosing?.LeftoverReadyBalls ?? availability.RegularReadyBalls,
            LeftoverAttentionBalls = existingClosing?.LeftoverAttentionBalls ?? availability.AttentionAvailableBalls + availability.MustUseNextDayBalls,
            LeftoverMixedLoads = existingClosing?.LeftoverMixedLoads ?? weeklyGoal.MixedButNotBalledLoads,
            Notes = request.Notes ?? "Weekly closing preview overlaid from persisted operational slice drafts.",
            Reason = "Preview generated from persisted production, daily closing, and event drafts."
        };
        ApplyCarryoverConsistencyWarnings(weeklyCorrectionProposal, availability, weeklyGoal, warnings);

        var intent = new WeeklyClosingIntent(
            $"Weekly closing preview for operational week {request.WeekStartDate:yyyy-MM-dd} through {request.ReferenceDate:yyyy-MM-dd}.",
            $"Weekly closing preview for operational week {request.WeekStartDate:yyyy-MM-dd} through {request.ReferenceDate:yyyy-MM-dd}.",
            request.ReferenceDate,
            closingWeekStartDate,
            null,
            weeklyCorrectionProposal.LeftoverReadyBalls,
            weeklyCorrectionProposal.LeftoverMixedLoads,
            weeklyCorrectionProposal.LeftoverMixedLoads == 0,
            false,
            weeklyCorrectionProposal.Reason);

        var beforeSnapshot = new
        {
            request.ReferenceDate,
            OperationalWeekStartDate = request.WeekStartDate,
            WeeklyClosingWeekStartDate = closingWeekStartDate,
            ExistingWeeklyClosingId = existingClosing?.Id,
            LiveActualUsedBalls = weeklyGoal.ActualUsedBallsThisWeek,
            ExistingDailyClosingTotal = dailyClosingWeekSummary.TotalActualUsedBalls,
            DraftDailyClosingTotal = draftDailyClosingTotals,
            ProductionDraftCount = productionDrafts.Length,
            EventDraftCount = eventDrafts.Length,
            CarryoverReadyBalls = carryover.CarryoverReadyBalls,
            ReadyNowBalls = weeklyGoal.ReadyNowBalls
        };
        var afterPreview = new
        {
            weeklyCorrectionProposal,
            DailyClosingDraftCount = dailyClosingDrafts.Length,
            EventDraftCount = eventDrafts.Length,
            ProductionDraftCount = productionDrafts.Length
        };
        var diffEntries = new object[]
        {
            new
            {
                Field = "UsedBalls",
                Before = existingClosing?.UsedBalls ?? weeklyGoal.ActualUsedBallsThisWeek,
                After = weeklyCorrectionProposal.UsedBalls
            },
            new
            {
                Field = "UsageSource",
                Before = "Live task-derived weekly usage",
                After = "Persisted daily closing drafts"
            }
        };

        var simulation = new OperationalSimulationResult
        {
            CorrelationId = correlationId,
            SourceText = intent.SourceText,
            Intent = intent,
            BeforeSnapshotJson = Serialize(beforeSnapshot),
            AfterPreviewJson = Serialize(afterPreview),
            DiffJson = Serialize(diffEntries),
            ValidationWarningsJson = Serialize(warnings),
            ValidationWarnings = warnings,
            WeeklyCorrectionProposal = weeklyCorrectionProposal,
            ExistingWeeklyClosing = existingClosing,
            Carryover = carryover,
            Availability = availability,
            WeeklyGoal = weeklyGoal,
            InventoryImpact = inventoryImpact
        };

        await PersistSimulationAuditAsync(simulation, actorUserId, cancellationToken);
        return simulation;
    }

    public async Task<OperationalSimulationResult> ReplayDraftAsync(
        OperationalDraft draft,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var previousSuppressionState = AuditPersistenceSuppressed.Value;
        AuditPersistenceSuppressed.Value = true;

        try
        {
            return draft.DraftType switch
            {
                WeeklyCorrectionDraftType => await SimulateAsync(
                    BuildWeeklyCorrectionReplayRequest(draft),
                    cancellationToken),
                WeeklyClosingPreviewDraftType => await SimulateWeeklyClosingPreviewAsync(
                    BuildWeeklyClosingPreviewReplayRequest(draft),
                    cancellationToken),
                DoughTaskDraftType => await SimulateDoughTaskAsync(
                    BuildDoughTaskReplayRequest(draft),
                    cancellationToken),
                DailyClosingDraftType => await SimulateDailyClosingAsync(
                    BuildDailyClosingReplayRequest(draft),
                    cancellationToken),
                RestaurantEventDraftType => await SimulateRestaurantEventAsync(
                    BuildRestaurantEventReplayRequest(draft),
                    cancellationToken),
                ProjectionAdjustmentDraftType => await SimulateOperationalProjectionAsync(
                    BuildProjectionReplayRequest(draft),
                    cancellationToken),
                _ => throw new InvalidOperationException($"The draft type '{draft.DraftType}' cannot be replayed for preview.")
            };
        }
        finally
        {
            AuditPersistenceSuppressed.Value = previousSuppressionState;
        }
    }

    private static (
        object BeforeSnapshot,
        object AfterPreview,
        IReadOnlyList<object> DiffEntries,
        WeeklyCorrectionProposal? WeeklyCorrectionProposal,
        DoughTaskDraftProposal? DoughTaskDraftProposal)
        BuildPreview(
            OperationalIntent intent,
            DateOnly referenceDate,
            WeeklyDoughClosingResponse? existingClosing,
            OperationalDraft? existingDraft,
            WeeklyDoughCarryoverResponse carryover,
            Contracts.Responses.Dough.DoughAvailabilityProjectionResponse availability,
            Contracts.Responses.Prep.WeeklyDoughCalendarResponse weeklyGoal,
            Contracts.Responses.Dough.DoughInventoryImpactResponse inventoryImpact,
            PrepItem? doughItem,
            IList<OperationalValidationWarning> warnings)
    {
        if (intent is WeeklyClosingIntent weeklyClosingIntent)
        {
            var proposedReadyBalls = weeklyClosingIntent.LeftoverReadyBalls ??
                existingClosing?.LeftoverReadyBalls ??
                availability.RegularReadyBalls;
            var proposedMixedLoads = weeklyClosingIntent.LeftoverMixedLoads ??
                existingClosing?.LeftoverMixedLoads ??
                weeklyGoal.MixedButNotBalledLoads;
            var proposedAttentionBalls = existingClosing?.LeftoverAttentionBalls ??
                availability.AttentionAvailableBalls + availability.MustUseNextDayBalls;
            var weeklyCorrectionProposal = new WeeklyCorrectionProposal
            {
                ExistingWeeklyClosingId = existingClosing?.Id,
                WeekStartDate = weeklyClosingIntent.WeekStartDate,
                NeededBalls = existingClosing?.NeededBalls ?? weeklyGoal.WeekTotalNeededBalls,
                ProducedBalls = existingClosing?.ProducedBalls ?? availability.ProducedThisWeekBalls,
                UsedBalls = existingClosing?.UsedBalls ?? availability.ActualUsedBallsThisWeek,
                LostBalls = existingClosing?.LostBalls ?? availability.LostBallsThisWeek,
                LeftoverReadyBalls = proposedReadyBalls,
                LeftoverAttentionBalls = proposedAttentionBalls,
                LeftoverMixedLoads = proposedMixedLoads,
                Notes = existingClosing?.Notes ?? $"Drafted from operational narrative on {referenceDate:yyyy-MM-dd}.",
                Reason = weeklyClosingIntent.CorrectionReason
            };
            var doughTaskDraftProposal = doughItem is null || !weeklyClosingIntent.SundayLoadBalledMonday
                ? null
                : new DoughTaskDraftProposal
                {
                    TaskDate = referenceDate.AddDays(1),
                    TaskType = nameof(PrepTaskType.BallDough),
                    Quantity = DoughRules.StandardBatchBalls,
                    QuantityUnit = nameof(DoughQuantityUnit.Balls),
                    AssignedRole = nameof(ApplicationRole.PizzaMaker),
                    PrepItemId = doughItem.Id,
                    PrepStationId = doughItem.PrepStationId,
                    Notes = "Backfill Monday balling for the prior Sunday load before weekly closing correction.",
                    AutoCompleteOnApproval = true,
                    CompletionQuantity = DoughRules.StandardBatchBalls
                };

            if (weeklyClosingIntent.SundayLoadBalledMonday && proposedMixedLoads > 0)
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "monday-balling-still-pending",
                    Message = "The narrative says the Sunday load was balled on Monday, so mixed loads should not remain pending.",
                    BlocksDraft = true
                });
            }

            if (weeklyClosingIntent.LinesLeftover.HasValue &&
                proposedReadyBalls != weeklyClosingIntent.LinesLeftover.Value * DoughRules.StandardBatchBalls)
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "line-conversion-mismatch",
                    Message = "The proposed ready balls do not match the current line-to-ball conversion.",
                    BlocksDraft = true
                });
            }

            var beforeSnapshot = new
            {
                ReferenceDate = referenceDate,
                WeekStartDate = weeklyClosingIntent.WeekStartDate,
                ExistingDraftId = existingDraft?.Id,
                ExistingDraftStatus = existingDraft?.Status.ToString(),
                ExistingWeeklyClosingId = existingClosing?.Id,
                ExistingLeftoverReadyBalls = existingClosing?.LeftoverReadyBalls,
                ExistingLeftoverAttentionBalls = existingClosing?.LeftoverAttentionBalls,
                ExistingLeftoverMixedLoads = existingClosing?.LeftoverMixedLoads,
                ExistingCorrectionNote = existingClosing?.CorrectionNote,
                CarryoverReadyBalls = carryover.CarryoverReadyBalls,
                CarryoverMixedLoads = carryover.MixedButNotBalledLoads,
                ReadyNowBalls = weeklyGoal.ReadyNowBalls,
                AttentionAvailableBalls = availability.AttentionAvailableBalls,
                MustUseNextDayBalls = availability.MustUseNextDayBalls,
                MixedButNotBalledLoads = weeklyGoal.MixedButNotBalledLoads,
                MixedButNotBalledBalls = weeklyGoal.MixedButNotBalledBalls,
                InventoryReadyNowBalls = inventoryImpact.ReadyNowBalls
            };
            var afterPreview = new
            {
                weeklyCorrectionProposal,
                doughTaskDraftProposal
            };
            var diffEntries = BuildWeeklyCorrectionDiffEntries(existingClosing, weeklyCorrectionProposal);

            return (beforeSnapshot, afterPreview, diffEntries, weeklyCorrectionProposal, doughTaskDraftProposal);
        }

        if (intent is ProductionIntent productionIntent)
        {
            var doughTaskDraftProposal = new DoughTaskDraftProposal
            {
                TaskDate = referenceDate,
                TaskType = productionIntent.MentionsBalling ? nameof(PrepTaskType.BallDough) : nameof(PrepTaskType.MakeDoughLoad),
                Quantity = productionIntent.Quantity ?? DoughRules.StandardBatchBalls,
                QuantityUnit = productionIntent.MentionsBalling
                    ? nameof(DoughQuantityUnit.Balls)
                    : nameof(DoughQuantityUnit.FullLoads),
                AssignedRole = nameof(ApplicationRole.PizzaMaker),
                PrepItemId = doughItem?.Id ?? Guid.Empty,
                PrepStationId = doughItem?.PrepStationId ?? Guid.Empty,
                Notes = productionIntent.Notes ?? "Drafted from production narrative.",
                AutoCompleteOnApproval = false
            };
            var beforeSnapshot = new
            {
                ReferenceDate = referenceDate,
                ExistingDraftId = existingDraft?.Id,
                ExistingDraftStatus = existingDraft?.Status.ToString(),
                ReadyNowBalls = weeklyGoal.ReadyNowBalls,
                FutureBalls = weeklyGoal.FutureBalls,
                StillMissingThisWeekBalls = weeklyGoal.StillMissingThisWeekBalls
            };
            var afterPreview = new
            {
                doughTaskDraftProposal
            };
            var diffEntries = new object[]
            {
                new
                {
                    Field = "TaskBackfill",
                    Before = "Not drafted",
                    After = $"{doughTaskDraftProposal.TaskType} on {doughTaskDraftProposal.TaskDate:yyyy-MM-dd}"
                }
            };

            return (beforeSnapshot, afterPreview, diffEntries, null, doughTaskDraftProposal);
        }

        warnings.Add(new OperationalValidationWarning
        {
            Code = "unsupported-intent",
            Message = "The current MVP can explain this narrative, but it does not yet generate a draft proposal for this intent."
        });

        var genericBefore = new
        {
            ReferenceDate = referenceDate,
            ExistingDraftId = existingDraft?.Id,
            ExistingDraftStatus = existingDraft?.Status.ToString(),
            weeklyGoal.ReadyNowBalls,
            weeklyGoal.StillMissingThisWeekBalls,
            availability.AvailableBalls,
            inventoryImpact.RemainingTrackedBalls
        };
        var genericAfter = new
        {
            Intent = intent.Kind.ToString(),
            intent.NormalizedSummary
        };

        return (genericBefore, genericAfter, Array.Empty<object>(), null, null);
    }

    private static void ApplyExistingDraftWarnings(
        OperationalDraft? existingDraft,
        IList<OperationalValidationWarning> warnings)
    {
        if (existingDraft is null)
        {
            return;
        }

        if (existingDraft.Status is OperationalDraftStatus.Pending or OperationalDraftStatus.ReadyForApproval)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "existing-draft-context",
                Message = $"A persisted draft already exists for this correlation id with status {existingDraft.Status}. Review it before creating another draft.",
                RequiresHumanReview = true
            });
        }
    }

    private static void ApplyExistingWeeklyClosingWarnings(
        WeeklyDoughClosingResponse? existingClosing,
        IList<OperationalValidationWarning> warnings)
    {
        if (existingClosing is null)
        {
            return;
        }

        warnings.Add(new OperationalValidationWarning
        {
            Code = "existing-weekly-closing",
            Message = "A weekly closing already exists for this week, so any approved draft must be treated as a correction and reviewed carefully.",
            RequiresHumanReview = true
        });
    }

    private static void ApplyDuplicateLoadPreventionWarnings(
        WeeklyCorrectionProposal? weeklyCorrectionProposal,
        DoughTaskDraftProposal? doughTaskDraftProposal,
        Contracts.Responses.Prep.WeeklyDoughCalendarResponse weeklyGoal,
        IReadOnlyList<PrepTask> tasks,
        IReadOnlyCollection<DoughBatch> batches,
        IList<OperationalValidationWarning> warnings)
    {
        var activeUnballedLoads = batches.Count(batch => !batch.IsVoided && !batch.IsBalled);

        if (weeklyCorrectionProposal is not null &&
            weeklyCorrectionProposal.LeftoverMixedLoads > weeklyGoal.MixedButNotBalledLoads)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "duplicate-load-prevention",
                Message = "The proposed mixed loads exceed the task-derived mixed loads currently recognized by the system, which risks counting the same physical dough twice.",
                BlocksDraft = true
            });
        }

        if (weeklyCorrectionProposal is not null &&
            weeklyCorrectionProposal.LeftoverMixedLoads > activeUnballedLoads)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "mixed-load-physical-mismatch",
                Message = "The proposed mixed carryover is greater than the unballed physical loads currently tracked, so the same dough may be counted twice.",
                BlocksDraft = true
            });
        }

        if (doughTaskDraftProposal is null)
        {
            return;
        }

        var duplicateTaskExists = tasks.Any(task =>
            task.Id != doughTaskDraftProposal.ExistingPrepTaskId &&
            task.TaskType.ToString() == doughTaskDraftProposal.TaskType &&
            task.TaskDate == doughTaskDraftProposal.TaskDate &&
            task.Status != PrepTaskStatus.Cancelled &&
            (task.QuantityRecommended == doughTaskDraftProposal.Quantity ||
             task.QuantityCompleted == doughTaskDraftProposal.Quantity));

        if (duplicateTaskExists)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "duplicate-task-draft",
                Message = "A task with the same type, date, and quantity already exists. Creating another draft would duplicate operational work.",
                BlocksDraft = true
            });
        }

        var duplicateBalledLoadExists = doughTaskDraftProposal.TaskType == nameof(PrepTaskType.BallDough) &&
            batches.Any(batch =>
                !batch.IsVoided &&
                batch.IsBalled &&
                batch.BatchDate.AddDays(1) == doughTaskDraftProposal.TaskDate &&
                batch.TotalBalls == doughTaskDraftProposal.Quantity);

        if (duplicateBalledLoadExists)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "duplicate-balled-load",
                Message = "The draft would ball a dough load that is already marked as balled in the tracked physical batches.",
                BlocksDraft = true
            });
        }
    }

    private static PrepTask? ResolveReusableExistingTask(
        OperationalDoughTaskDraftRequest request,
        string normalizedTaskType,
        IReadOnlyList<PrepTask> tasks,
        IList<OperationalValidationWarning> warnings)
    {
        if (!request.AutoCompleteOnApproval)
        {
            return null;
        }

        var matchingOpenTasks = tasks
            .Where(task =>
                task.TaskType.ToString() == normalizedTaskType &&
                task.TaskDate == request.TaskDate &&
                task.Status is PrepTaskStatus.Pending or PrepTaskStatus.InProgress &&
                (task.QuantityRecommended == request.QuantityValue ||
                 task.QuantityCompleted == request.QuantityValue))
            .ToArray();

        if (request.ExistingPrepTaskId.HasValue)
        {
            var explicitMatch = matchingOpenTasks.FirstOrDefault(task => task.Id == request.ExistingPrepTaskId.Value);
            if (explicitMatch is not null)
            {
                return explicitMatch;
            }

            warnings.Add(new OperationalValidationWarning
            {
                Code = "existing-task-not-reusable",
                Message = "The requested prep task target could not be reused because it is missing, completed, cancelled, or no longer matches this structured completion draft.",
                BlocksDraft = true
            });
            return null;
        }

        if (matchingOpenTasks.Length <= 1)
        {
            return matchingOpenTasks.SingleOrDefault();
        }

        warnings.Add(new OperationalValidationWarning
        {
            Code = "multiple-open-task-matches",
            Message = "Multiple open prep tasks match this structured completion entry, so a human must choose the correct task before approval.",
            BlocksDraft = true
        });
        return null;
    }

    private static void ApplyCarryoverConsistencyWarnings(
        WeeklyCorrectionProposal? weeklyCorrectionProposal,
        Contracts.Responses.Dough.DoughAvailabilityProjectionResponse availability,
        Contracts.Responses.Prep.WeeklyDoughCalendarResponse weeklyGoal,
        IList<OperationalValidationWarning> warnings)
    {
        if (weeklyCorrectionProposal is null)
        {
            return;
        }

        var proposedPhysicalLeftoverBalls =
            weeklyCorrectionProposal.LeftoverReadyBalls +
            weeklyCorrectionProposal.LeftoverAttentionBalls +
            (weeklyCorrectionProposal.LeftoverMixedLoads * DoughRules.StandardBatchBalls);
        var derivedPhysicalLeftoverBalls =
            weeklyGoal.ReadyNowBalls +
            weeklyGoal.MixedButNotBalledBalls +
            weeklyGoal.StillFermentingBalls;

        if (proposedPhysicalLeftoverBalls > derivedPhysicalLeftoverBalls)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "carryover-consistency",
                Message = "The proposed leftover dough exceeds the task-derived physical dough currently available in the system.",
                BlocksDraft = true
            });
        }

        var accountedPhysicalBalls =
            weeklyGoal.ReadyNowBalls +
            weeklyGoal.MixedButNotBalledBalls +
            weeklyGoal.StillFermentingBalls +
            weeklyGoal.ActualUsedBallsThisWeek;
        var traceablePhysicalBalls =
            weeklyGoal.CarryoverAvailableBalls +
            weeklyGoal.CarryoverMixedButNotBalledPotentialBalls +
            weeklyGoal.PreviousWeekFinishedBalls +
            weeklyGoal.ProducedThisWeekBalls -
            availability.LostBallsThisWeek;

        if (accountedPhysicalBalls > traceablePhysicalBalls)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "physical-consistency-overflow",
                Message = "Ready, mixed, fermenting, and used dough together exceed the traceable physical dough available from carryover plus production.",
                BlocksDraft = true
            });
        }
    }

    private static void ApplyWeeklyClosingConsistencyWarnings(
        WeeklyCorrectionProposal? weeklyCorrectionProposal,
        Contracts.Responses.Dough.DoughAvailabilityProjectionResponse availability,
        Contracts.Responses.Prep.WeeklyDoughCalendarResponse weeklyGoal,
        IList<OperationalValidationWarning> warnings)
    {
        if (weeklyCorrectionProposal is null)
        {
            return;
        }

        var proposedAvailableBalls =
            weeklyCorrectionProposal.LeftoverReadyBalls +
            weeklyCorrectionProposal.LeftoverAttentionBalls;
        var taskDerivedAvailableBalls =
            availability.RegularReadyBalls +
            availability.AttentionAvailableBalls +
            availability.MustUseNextDayBalls;

        if (proposedAvailableBalls != taskDerivedAvailableBalls)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "weekly-closing-available-mismatch",
                Message = "The proposed available carryover does not match the current task-derived available dough.",
                BlocksDraft = true
            });
        }

        if (weeklyCorrectionProposal.LeftoverMixedLoads != weeklyGoal.MixedButNotBalledLoads)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "weekly-closing-mixed-mismatch",
                Message = "The proposed mixed loads do not match the current task-derived mixed dough state.",
                BlocksDraft = true
            });
        }
    }

    private static void ApplyMissingCatalogWarnings(
        PrepItem? doughItem,
        DoughTaskDraftProposal? doughTaskDraftProposal,
        IList<OperationalValidationWarning> warnings)
    {
        if (doughTaskDraftProposal is null || doughItem is not null)
        {
            return;
        }

        warnings.Add(new OperationalValidationWarning
        {
            Code = "missing-dough-catalog-item",
            Message = "The DOUGH prep item is not configured, so the system cannot safely build an approval-ready dough task payload.",
            BlocksDraft = true
        });
    }

    private async Task<WeeklyDoughClosingResponse?> FindClosingForWeekAsync(
        DateOnly normalizedClosingWeekStartDate,
        CancellationToken cancellationToken)
    {
        var closings = await _weeklyDoughClosingReadService.GetWeeklyClosingsAsync(
            new GetWeeklyClosingsRequest
            {
                FromWeekStartDate = normalizedClosingWeekStartDate,
                ToWeekStartDate = normalizedClosingWeekStartDate.AddDays(1)
            },
            cancellationToken);

        return closings.FirstOrDefault(item =>
            item.WeekStartDate == normalizedClosingWeekStartDate ||
            item.WeekStartDate == normalizedClosingWeekStartDate.AddDays(1));
    }

    private static IReadOnlyList<object> BuildWeeklyCorrectionDiffEntries(
        WeeklyDoughClosingResponse? existingClosing,
        WeeklyCorrectionProposal proposal)
    {
        return
        [
            new
            {
                Field = "LeftoverReadyBalls",
                Before = existingClosing?.LeftoverReadyBalls,
                After = proposal.LeftoverReadyBalls
            },
            new
            {
                Field = "LeftoverAttentionBalls",
                Before = existingClosing?.LeftoverAttentionBalls,
                After = proposal.LeftoverAttentionBalls
            },
            new
            {
                Field = "LeftoverMixedLoads",
                Before = existingClosing?.LeftoverMixedLoads,
                After = proposal.LeftoverMixedLoads
            },
            new
            {
                Field = "CorrectionReason",
                Before = existingClosing?.CorrectionNote,
                After = proposal.Reason
            }
        ];
    }

    private async Task PersistSimulationAuditAsync(
        OperationalSimulationResult simulation,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        if (AuditPersistenceSuppressed.Value)
        {
            return;
        }

        var auditEntry = OperationalAuditEntry.Create(
            simulation.CorrelationId,
            "SimulateOperationalNarrative",
            actorUserId,
            simulation.SourceText,
            SerializeIntent(simulation.Intent),
            simulation.BeforeSnapshotJson,
            simulation.AfterPreviewJson,
            simulation.ValidationWarningsJson,
            draftId: null);

        await _operationalAuditEntryRepository.AddAsync(auditEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyExistingStructuredDraftWarnings(
        IReadOnlyList<OperationalDraft> existingDrafts,
        string draftType,
        Func<OperationalDraft, bool> predicate,
        string message,
        IList<OperationalValidationWarning> warnings)
    {
        if (existingDrafts.Any(draft =>
                string.Equals(draft.DraftType, draftType, StringComparison.Ordinal) &&
                draft.Status is OperationalDraftStatus.Pending or OperationalDraftStatus.ReadyForApproval &&
                predicate(draft)))
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "existing-structured-draft",
                Message = message,
                BlocksDraft = true
            });
        }
    }

    private static void ValidateUsageBreakdown(
        int actualUsedBalls,
        IReadOnlyList<OperationalUsageComponent> usageBreakdown,
        IList<OperationalValidationWarning> warnings)
    {
        if (actualUsedBalls < 0)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "negative-daily-usage",
                Message = "Daily closing usage cannot be negative.",
                BlocksDraft = true
            });
        }

        if (usageBreakdown.Any(component => component.Balls < 0))
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "negative-usage-component",
                Message = "Usage breakdown components cannot be negative.",
                BlocksDraft = true
            });
        }

        var breakdownTotal = usageBreakdown.Sum(component => component.Balls);
        if (usageBreakdown.Count > 0 && breakdownTotal != actualUsedBalls)
        {
            warnings.Add(new OperationalValidationWarning
            {
                Code = "daily-usage-breakdown-mismatch",
                Message = $"The daily closing breakdown sums to {breakdownTotal} balls but the draft total says {actualUsedBalls}.",
                BlocksDraft = true
            });
        }
    }

    private static T DeserializePayload<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"The operational draft payload for {typeof(T).Name} is invalid.");
    }

    private static OperationalNarrativeRequest BuildWeeklyCorrectionReplayRequest(OperationalDraft draft)
    {
        var payload = DeserializePayload<WeeklyCorrectionApprovalPayload>(draft.DraftPayloadJson);
        var referenceDate = ExtractDateOnlyProperty(draft.BeforeSnapshotJson, "ReferenceDate")
            ?? payload.WeekStartDate.AddDays(6);

        return new OperationalNarrativeRequest
        {
            CorrelationId = draft.CorrelationId,
            SourceText = draft.SourceText,
            ReferenceDate = referenceDate,
            TargetWeekStartDate = payload.WeekStartDate,
            HistoricalWeeksToUse = 8,
            ActorUserId = draft.CreatedBy
        };
    }

    private static OperationalWeeklyClosingPreviewRequest BuildWeeklyClosingPreviewReplayRequest(OperationalDraft draft)
    {
        var payload = DeserializePayload<WeeklyCorrectionApprovalPayload>(draft.DraftPayloadJson);
        var referenceDate = ExtractDateOnlyProperty(draft.BeforeSnapshotJson, "ReferenceDate")
            ?? payload.WeekStartDate.AddDays(6);
        var operationalWeekStartDate = ExtractDateOnlyProperty(draft.BeforeSnapshotJson, "OperationalWeekStartDate")
            ?? payload.WeekStartDate;
        var notes = ExtractNestedStringProperty(draft.AfterPreviewJson, "weeklyCorrectionProposal", "notes")
            ?? payload.Notes
            ?? "Preview replay from persisted draft.";

        return new OperationalWeeklyClosingPreviewRequest
        {
            CorrelationId = draft.CorrelationId,
            ReferenceDate = referenceDate,
            WeekStartDate = operationalWeekStartDate,
            HistoricalWeeksToUse = 8,
            Notes = notes,
            ActorUserId = draft.CreatedBy
        };
    }

    private static OperationalDoughTaskDraftRequest BuildDoughTaskReplayRequest(OperationalDraft draft)
    {
        var payload = DeserializePayload<DoughTaskApprovalPayload>(draft.DraftPayloadJson);

        return new OperationalDoughTaskDraftRequest
        {
            CorrelationId = draft.CorrelationId,
            ExistingPrepTaskId = payload.ExistingPrepTaskId,
            TaskDate = payload.TaskDate,
            HistoricalWeeksToUse = 8,
            TaskType = payload.TaskType,
            QuantityValue = payload.QuantityValue,
            QuantityUnit = payload.QuantityUnit,
            AssignedRole = payload.AssignedRole,
            AutoCompleteOnApproval = payload.AutoCompleteOnApproval,
            CompletionQuantityValue = payload.CompletionQuantityValue,
            Notes = payload.Notes,
            ActorUserId = draft.CreatedBy
        };
    }

    private static OperationalDailyClosingDraftRequest BuildDailyClosingReplayRequest(OperationalDraft draft)
    {
        var payload = DeserializePayload<DailyClosingApprovalPayload>(draft.DraftPayloadJson);

        return new OperationalDailyClosingDraftRequest
        {
            CorrelationId = draft.CorrelationId,
            ClosingDate = payload.ClosingDate,
            HistoricalWeeksToUse = 8,
            ActualUsedBalls = payload.ActualUsedBalls,
            UsageBreakdown = payload.UsageBreakdown,
            Notes = payload.Notes,
            ActorUserId = draft.CreatedBy
        };
    }

    private static OperationalEventDraftRequest BuildRestaurantEventReplayRequest(OperationalDraft draft)
    {
        var payload = DeserializePayload<RestaurantEventApprovalPayload>(draft.DraftPayloadJson);

        return new OperationalEventDraftRequest
        {
            CorrelationId = draft.CorrelationId,
            EventDate = payload.EventDate,
            Name = payload.Name,
            EstimatedDoughBalls = payload.EstimatedDoughBalls,
            ExpectedPeopleMinimum = payload.ExpectedPeopleMinimum,
            ExpectedPeopleMaximum = payload.ExpectedPeopleMaximum,
            PreviousNarrativeDoughBalls = payload.PreviousNarrativeDoughBalls,
            AllowShortFermentation = payload.AllowShortFermentation,
            Notes = payload.Notes,
            ActorUserId = draft.CreatedBy
        };
    }

    private static OperationalProjectionRequest BuildProjectionReplayRequest(OperationalDraft draft)
    {
        var payload = DeserializePayload<ProjectionAdjustmentDraftPayload>(draft.DraftPayloadJson);

        return new OperationalProjectionRequest
        {
            CorrelationId = draft.CorrelationId,
            ReferenceDate = payload.ReferenceDate,
            HistoricalWeeksToUse = 8,
            Notes = payload.Notes,
            ActorUserId = draft.CreatedBy
        };
    }

    private static DateOnly? ExtractDateOnlyProperty(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        if (!TryGetProperty(document.RootElement, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateOnly.TryParse(property.GetString(), out var value)
            ? value
            : null;
    }

    private static string? ExtractNestedStringProperty(string json, string parentPropertyName, string childPropertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        if (!TryGetProperty(document.RootElement, parentPropertyName, out var parent) ||
            parent.ValueKind != JsonValueKind.Object ||
            !TryGetProperty(parent, childPropertyName, out var child) ||
            child.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return child.GetString();
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static string NormalizeTaskType(string taskType)
    {
        if (!PrepTaskTypeExtensions.TryParse(taskType, out var parsedTaskType))
        {
            throw new ArgumentException("Task type is not valid for dough task drafts.", nameof(taskType));
        }

        return parsedTaskType.ToString();
    }

    private static string NormalizeQuantityUnit(string quantityUnit)
    {
        if (!Enum.TryParse<DoughQuantityUnit>(quantityUnit, true, out var parsedQuantityUnit))
        {
            throw new ArgumentException("Quantity unit is not valid for dough task drafts.", nameof(quantityUnit));
        }

        return parsedQuantityUnit.ToString();
    }

    private static string NormalizeAssignedRole(string assignedRole)
    {
        if (!ApplicationRoleExtensions.TryParse(assignedRole, out var parsedRole) || parsedRole == ApplicationRole.Pending)
        {
            throw new ArgumentException("Assigned role is not valid for dough task drafts.", nameof(assignedRole));
        }

        return parsedRole.GetCanonicalName();
    }

    private static string BuildDoughTaskSourceText(DoughTaskDraftProposal proposal)
    {
        return $"{proposal.TaskType} {proposal.Quantity} {proposal.QuantityUnit} on {proposal.TaskDate:yyyy-MM-dd}.";
    }

    private static string BuildDailyClosingSourceText(
        DateOnly closingDate,
        int actualUsedBalls,
        IReadOnlyList<OperationalUsageComponent> usageBreakdown)
    {
        var breakdown = usageBreakdown.Count == 0
            ? "No structured breakdown provided"
            : string.Join(", ", usageBreakdown.Select(component =>
                string.IsNullOrWhiteSpace(component.ReferenceName)
                    ? $"{component.Category} {component.Balls}"
                    : $"{component.Category} {component.ReferenceName} {component.Balls}"));

        return $"Daily closing {closingDate:yyyy-MM-dd}: {actualUsedBalls} balls used. Breakdown: {breakdown}.";
    }

    private static string BuildDailyClosingNotes(IReadOnlyList<OperationalUsageComponent> usageBreakdown)
    {
        return usageBreakdown.Count == 0
            ? "Structured daily closing draft."
            : $"Breakdown: {string.Join(", ", usageBreakdown.Select(component => $"{component.Category} {component.Balls}"))}.";
    }

    private static string BuildRestaurantEventSourceText(OperationalEventDraftRequest request)
    {
        return $"External event {request.Name} on {request.EventDate:yyyy-MM-dd}: {request.EstimatedDoughBalls} balls for {request.ExpectedPeopleMinimum}-{request.ExpectedPeopleMaximum} people.";
    }

    private static string BuildProjectionSourceText(OperationalProjectionResult projection)
    {
        return $"Operational projection for {projection.ReferenceDate:yyyy-MM-dd}: ReadyNow {projection.ReadyNowBalls}, balls ready for service {projection.BallsReadyForService}, projected shortage {projection.ProjectedShortageBalls}.";
    }

    private static string ResolveActorUserId(string? actorUserId)
    {
        return string.IsNullOrWhiteSpace(actorUserId)
            ? "mcp-simulate"
            : actorUserId.Trim();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string SerializeIntent(OperationalIntent intent)
    {
        return JsonSerializer.Serialize(intent, intent.GetType(), JsonOptions);
    }

    private static DateOnly NormalizeClosingWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }
}
