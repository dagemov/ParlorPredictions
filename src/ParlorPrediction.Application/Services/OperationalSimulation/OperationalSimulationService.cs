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

    private readonly IDoughAvailabilityProjectionService _doughAvailabilityProjectionService;
    private readonly IDoughBatchReadRepository _doughBatchReadRepository;
    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;
    private readonly IOperationalAuditEntryRepository _operationalAuditEntryRepository;
    private readonly IOperationalDraftRepository _operationalDraftRepository;
    private readonly IOperationalIntentClassifier _operationalIntentClassifier;
    private readonly IPrepItemReadRepository _prepItemReadRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public OperationalSimulationService(
        IDoughAvailabilityProjectionService doughAvailabilityProjectionService,
        IDoughBatchReadRepository doughBatchReadRepository,
        IDoughInventoryImpactReadService doughInventoryImpactReadService,
        IOperationalAuditEntryRepository operationalAuditEntryRepository,
        IOperationalDraftRepository operationalDraftRepository,
        IOperationalIntentClassifier operationalIntentClassifier,
        IPrepItemReadRepository prepItemReadRepository,
        IPrepTaskRepository prepTaskRepository,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService,
        IUnitOfWork unitOfWork,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _doughAvailabilityProjectionService = doughAvailabilityProjectionService;
        _doughBatchReadRepository = doughBatchReadRepository;
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
        _operationalAuditEntryRepository = operationalAuditEntryRepository;
        _operationalDraftRepository = operationalDraftRepository;
        _operationalIntentClassifier = operationalIntentClassifier;
        _prepItemReadRepository = prepItemReadRepository;
        _prepTaskRepository = prepTaskRepository;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
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

        return simulation;
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
