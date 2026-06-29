using System.Text.Json;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.OperationalSimulation;

public sealed class OperationalSimulationService : IOperationalSimulationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDoughAvailabilityProjectionService _doughAvailabilityProjectionService;
    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;
    private readonly IOperationalIntentClassifier _operationalIntentClassifier;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public OperationalSimulationService(
        IDoughAvailabilityProjectionService doughAvailabilityProjectionService,
        IDoughInventoryImpactReadService doughInventoryImpactReadService,
        IOperationalIntentClassifier operationalIntentClassifier,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _doughAvailabilityProjectionService = doughAvailabilityProjectionService;
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
        _operationalIntentClassifier = operationalIntentClassifier;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
    }

    public async Task<OperationalSimulationResult> SimulateAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceText);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
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
        var existingClosing = await FindClosingForWeekAsync(
            targetWeekStartDate,
            cancellationToken);
        var warnings = new List<OperationalValidationWarning>();

        var (beforeSnapshot, afterPreview, diffEntries, weeklyCorrectionProposal, doughTaskDraftProposal) =
            BuildPreview(
                intent,
                request.ReferenceDate,
                existingClosing,
                carryover,
                availability,
                weeklyGoal,
                inventoryImpact,
                warnings);

        return new OperationalSimulationResult
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
            WeeklyDoughCarryoverResponse carryover,
            Contracts.Responses.Dough.DoughAvailabilityProjectionResponse availability,
            Contracts.Responses.Prep.WeeklyDoughCalendarResponse weeklyGoal,
            Contracts.Responses.Dough.DoughInventoryImpactResponse inventoryImpact,
            IList<OperationalValidationWarning> warnings)
    {
        if (intent is WeeklyClosingIntent weeklyClosingIntent)
        {
            var proposedReadyBalls = weeklyClosingIntent.LeftoverReadyBalls ??
                existingClosing?.LeftoverReadyBalls ??
                carryover.CarryoverReadyBalls;
            var proposedMixedLoads = weeklyClosingIntent.LeftoverMixedLoads ??
                existingClosing?.LeftoverMixedLoads ??
                carryover.MixedButNotBalledLoads;
            var weeklyCorrectionProposal = new WeeklyCorrectionProposal
            {
                WeekStartDate = weeklyClosingIntent.WeekStartDate,
                LeftoverReadyBalls = proposedReadyBalls,
                LeftoverMixedLoads = proposedMixedLoads,
                Reason = weeklyClosingIntent.CorrectionReason
            };
            var doughTaskDraftProposal = weeklyClosingIntent.SundayLoadBalledMonday
                ? new DoughTaskDraftProposal
                {
                    TaskDate = referenceDate,
                    TaskType = "BallDough",
                    Quantity = DoughRules.StandardBatchBalls,
                    QuantityUnit = "Balls",
                    Notes = "Backfill Monday balling for the prior Sunday load before weekly closing correction."
                }
                : null;

            if (existingClosing is not null)
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "existing-weekly-closing",
                    Message = "A weekly closing already exists for the requested week. Use correction flow instead of create flow."
                });
            }

            if (weeklyClosingIntent.SundayLoadBalledMonday && proposedMixedLoads > 0)
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "monday-balling-still-pending",
                    Message = "The narrative says the Sunday load was balled on Monday, so mixed loads should not remain pending."
                });
            }

            if (weeklyClosingIntent.LinesLeftover.HasValue &&
                proposedReadyBalls != weeklyClosingIntent.LinesLeftover.Value * DoughRules.StandardBatchBalls)
            {
                warnings.Add(new OperationalValidationWarning
                {
                    Code = "line-conversion-mismatch",
                    Message = "The proposed ready balls do not match the current line-to-ball conversion."
                });
            }

            var beforeSnapshot = new
            {
                ReferenceDate = referenceDate,
                WeekStartDate = weeklyClosingIntent.WeekStartDate,
                ExistingWeeklyClosingId = existingClosing?.Id,
                ExistingLeftoverReadyBalls = existingClosing?.LeftoverReadyBalls,
                ExistingLeftoverMixedLoads = existingClosing?.LeftoverMixedLoads,
                ExistingCorrectionNote = existingClosing?.CorrectionNote,
                CarryoverReadyBalls = carryover.CarryoverReadyBalls,
                CarryoverMixedLoads = carryover.MixedButNotBalledLoads,
                ReadyNowBalls = weeklyGoal.ReadyNowBalls,
                StillMissingThisWeekBalls = weeklyGoal.StillMissingThisWeekBalls,
                InventoryReadyNowBalls = inventoryImpact.ReadyNowBalls
            };
            var afterPreview = new
            {
                weeklyCorrectionProposal.WeekStartDate,
                weeklyCorrectionProposal.LeftoverReadyBalls,
                weeklyCorrectionProposal.LeftoverMixedLoads,
                weeklyCorrectionProposal.Reason,
                SundayLoadBalledMonday = weeklyClosingIntent.SundayLoadBalledMonday,
                doughTaskDraftProposal
            };
            var diffEntries = BuildDiffEntries(existingClosing, weeklyCorrectionProposal);

            return (beforeSnapshot, afterPreview, diffEntries, weeklyCorrectionProposal, doughTaskDraftProposal);
        }

        if (intent is ProductionIntent productionIntent)
        {
            var doughTaskDraftProposal = new DoughTaskDraftProposal
            {
                TaskDate = referenceDate,
                TaskType = productionIntent.MentionsBalling ? "BallDough" : "MakeDoughLoad",
                Quantity = productionIntent.Quantity ?? DoughRules.StandardBatchBalls,
                QuantityUnit = productionIntent.MentionsBalling ? "Balls" : "FullLoads",
                Notes = productionIntent.Notes ?? "Drafted from production narrative."
            };
            var beforeSnapshot = new
            {
                ReferenceDate = referenceDate,
                ReadyNowBalls = weeklyGoal.ReadyNowBalls,
                FutureBalls = weeklyGoal.FutureBalls,
                StillMissingThisWeekBalls = weeklyGoal.StillMissingThisWeekBalls
            };
            var afterPreview = new
            {
                doughTaskDraftProposal.TaskDate,
                doughTaskDraftProposal.TaskType,
                doughTaskDraftProposal.Quantity,
                doughTaskDraftProposal.QuantityUnit,
                doughTaskDraftProposal.Notes
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

    private static IReadOnlyList<object> BuildDiffEntries(
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

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static DateOnly NormalizeClosingWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }
}
