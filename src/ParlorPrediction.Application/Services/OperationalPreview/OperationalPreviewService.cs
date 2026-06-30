using System.Text.Json;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.OperationalPreview;

public sealed class OperationalPreviewService : IOperationalPreviewService
{
    private const string WeeklyCorrectionDraftType = "WeeklyCorrection";
    private const string WeeklyClosingPreviewDraftType = "WeeklyClosingPreview";
    private const string DoughTaskDraftType = "DoughTask";
    private const string DailyClosingDraftType = "DailyClosing";
    private const string RestaurantEventDraftType = "RestaurantEvent";
    private const string ProjectionAdjustmentDraftType = "ProjectionAdjustment";
    private const string StateDriftDetectedCode = "STATE_DRIFT_DETECTED";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOperationalDraftRepository _operationalDraftRepository;
    private readonly IOperationalProjectionService _operationalProjectionService;
    private readonly IOperationalSimulationService _operationalSimulationService;

    public OperationalPreviewService(
        IOperationalDraftRepository operationalDraftRepository,
        IOperationalProjectionService operationalProjectionService,
        IOperationalSimulationService operationalSimulationService)
    {
        _operationalDraftRepository = operationalDraftRepository;
        _operationalProjectionService = operationalProjectionService;
        _operationalSimulationService = operationalSimulationService;
    }

    public async Task<OperationalPreviewResult> BuildPreviewAsync(
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        if (draftId == Guid.Empty)
        {
            throw new ArgumentException("Draft id is required.", nameof(draftId));
        }

        var draft = await _operationalDraftRepository.GetByIdAsync(draftId, cancellationToken)
            ?? throw new KeyNotFoundException("The operational draft could not be found.");
        var context = BuildContext(draft);
        var projection = await _operationalProjectionService.ProjectAsync(
            new OperationalProjectionRequest
            {
                CorrelationId = draft.CorrelationId,
                ReferenceDate = context.ReferenceDate,
                HistoricalWeeksToUse = context.HistoricalWeeksToUse,
                Notes = "Operational preview baseline.",
                ActorUserId = draft.CreatedBy
            },
            cancellationToken);
        var currentBaseState = BuildCurrentBaseState(projection);
        var hasPersistedSnapshot = HasPersistedSnapshot(draft);
        var persistedBeforeState = BuildPersistedBeforeState(draft, currentBaseState);
        var stateDriftDetected = hasPersistedSnapshot && HasStateDrift(persistedBeforeState, currentBaseState);

        if (hasPersistedSnapshot && !stateDriftDetected)
        {
            var persistedWarnings = DeserializeWarnings(draft.ValidationWarningsJson);
            var persistedAfterState = BuildPersistedAfterState(draft, persistedBeforeState);

            return BuildResult(
                draft,
                persistedBeforeState,
                persistedAfterState,
                persistedWarnings,
                usedPersistedSnapshot: true,
                stateDriftDetected: false);
        }

        var replay = await _operationalSimulationService.ReplayDraftAsync(draft, cancellationToken);
        var beforeState = BuildReplayBeforeState(draft, replay, currentBaseState);
        var afterState = BuildReplayAfterState(draft, replay, beforeState);
        var replayWarnings = MergeWarnings(
            replay.ValidationWarnings,
            stateDriftDetected
                ? new[]
                {
                    new OperationalValidationWarning
                    {
                        Code = StateDriftDetectedCode,
                        Message = "Current system state differs from the persisted draft snapshot. Preview was regenerated from live simulation.",
                        RequiresHumanReview = true
                    }
                }
                : Array.Empty<OperationalValidationWarning>());

        return BuildResult(
            draft,
            beforeState,
            afterState,
            replayWarnings,
            usedPersistedSnapshot: false,
            stateDriftDetected: stateDriftDetected);
    }

    private static OperationalPreviewResult BuildResult(
        OperationalDraft draft,
        PreviewState before,
        PreviewState after,
        IReadOnlyList<OperationalValidationWarning> warnings,
        bool usedPersistedSnapshot,
        bool stateDriftDetected)
    {
        var diff = BuildDiff(before, after);
        var hasConflicts = HasConflicts(after, warnings);
        var riskLevel = CalculateRiskLevel(warnings, hasConflicts);

        return new OperationalPreviewResult
        {
            DraftId = draft.Id,
            CorrelationId = draft.CorrelationId,
            Before = before,
            After = after,
            Diff = diff,
            ValidationWarnings = warnings,
            Warnings = warnings.Select(FormatWarning).ToArray(),
            HasConflicts = hasConflicts,
            RiskLevel = riskLevel,
            UsedPersistedSnapshot = usedPersistedSnapshot,
            StateDriftDetected = stateDriftDetected
        };
    }

    private static DraftPreviewContext BuildContext(OperationalDraft draft)
    {
        return draft.DraftType switch
        {
            WeeklyCorrectionDraftType => new DraftPreviewContext(
                ExtractDateOnlyProperty(draft.BeforeSnapshotJson, "ReferenceDate")
                ?? DeserializePayload<WeeklyCorrectionApprovalPayload>(draft.DraftPayloadJson).WeekStartDate.AddDays(6),
                8),
            WeeklyClosingPreviewDraftType => new DraftPreviewContext(
                ExtractDateOnlyProperty(draft.BeforeSnapshotJson, "ReferenceDate")
                ?? DeserializePayload<WeeklyCorrectionApprovalPayload>(draft.DraftPayloadJson).WeekStartDate.AddDays(6),
                8),
            DoughTaskDraftType => new DraftPreviewContext(
                DeserializePayload<DoughTaskApprovalPayload>(draft.DraftPayloadJson).TaskDate,
                8),
            DailyClosingDraftType => new DraftPreviewContext(
                DeserializePayload<DailyClosingApprovalPayload>(draft.DraftPayloadJson).ClosingDate,
                8),
            RestaurantEventDraftType => new DraftPreviewContext(
                DeserializePayload<RestaurantEventApprovalPayload>(draft.DraftPayloadJson).EventDate,
                8),
            ProjectionAdjustmentDraftType => new DraftPreviewContext(
                DeserializePayload<ProjectionAdjustmentDraftPayload>(draft.DraftPayloadJson).ReferenceDate,
                8),
            _ => throw new InvalidOperationException($"The draft type '{draft.DraftType}' is not supported by the preview layer.")
        };
    }

    private static PreviewState BuildCurrentBaseState(OperationalProjectionResult projection)
    {
        return new PreviewState
        {
            ReadyNowBalls = projection.ReadyNowBalls,
            WeeklyUsedBalls = projection.Days.Sum(day => day.ActualUsedBalls ?? 0),
            ProductionBalls = projection.ProductionLedger.BallsCompleted,
            ExternalEventConsumption = projection.ConsumptionLedger.EventBalls,
            BallsReadyForService = projection.BallsReadyForService
        };
    }

    private static PreviewState BuildPersistedBeforeState(
        OperationalDraft draft,
        PreviewState currentBaseState)
    {
        var readyNowBalls = ExtractIntProperty(draft.BeforeSnapshotJson, "ReadyNowBalls")
            ?? currentBaseState.ReadyNowBalls;
        var weeklyUsedBalls = ExtractIntProperty(draft.BeforeSnapshotJson, "LiveActualUsedBalls")
            ?? ExtractIntProperty(draft.BeforeSnapshotJson, "TotalActualUsedBalls")
            ?? ExtractIntProperty(draft.BeforeSnapshotJson, "ExistingActualUsedBalls")
            ?? currentBaseState.WeeklyUsedBalls;
        var productionBalls = ExtractIntProperty(draft.BeforeSnapshotJson, "ProducedBalls")
            ?? currentBaseState.ProductionBalls;
        var externalEventConsumption = ExtractIntProperty(draft.BeforeSnapshotJson, "ExistingEstimatedDoughBalls")
            ?? ExtractIntProperty(draft.BeforeSnapshotJson, "PreviousNarrativeDoughBalls")
            ?? currentBaseState.ExternalEventConsumption;
        var ballsReadyForService = ExtractIntProperty(draft.BeforeSnapshotJson, "AvailableBalls")
            ?? ExtractIntProperty(draft.BeforeSnapshotJson, "ReadyNowBalls")
            ?? ExtractIntProperty(draft.BeforeSnapshotJson, "CarryoverReadyBalls")
            ?? readyNowBalls;

        return new PreviewState
        {
            ReadyNowBalls = readyNowBalls,
            WeeklyUsedBalls = weeklyUsedBalls,
            ProductionBalls = productionBalls,
            ExternalEventConsumption = externalEventConsumption,
            BallsReadyForService = ballsReadyForService
        };
    }

    private static PreviewState BuildPersistedAfterState(
        OperationalDraft draft,
        PreviewState beforeState)
    {
        return draft.DraftType switch
        {
            WeeklyCorrectionDraftType => BuildWeeklyCorrectionAfterState(
                beforeState,
                DeserializePayload<WeeklyCorrectionApprovalPayload>(draft.DraftPayloadJson)),
            WeeklyClosingPreviewDraftType => BuildWeeklyCorrectionAfterState(
                beforeState,
                DeserializePayload<WeeklyCorrectionApprovalPayload>(draft.DraftPayloadJson)),
            DoughTaskDraftType => BuildDoughTaskAfterState(
                beforeState,
                DeserializePayload<DoughTaskApprovalPayload>(draft.DraftPayloadJson)),
            DailyClosingDraftType => BuildDailyClosingAfterState(
                beforeState,
                DeserializePayload<DailyClosingApprovalPayload>(draft.DraftPayloadJson),
                ExtractIntProperty(draft.BeforeSnapshotJson, "ExistingActualUsedBalls")),
            RestaurantEventDraftType => BuildRestaurantEventAfterState(
                beforeState,
                DeserializePayload<RestaurantEventApprovalPayload>(draft.DraftPayloadJson),
                ExtractIntProperty(draft.BeforeSnapshotJson, "ExistingEstimatedDoughBalls")),
            ProjectionAdjustmentDraftType => BuildProjectionAdjustmentAfterState(
                beforeState,
                DeserializePayload<ProjectionAdjustmentDraftPayload>(draft.DraftPayloadJson)),
            _ => beforeState
        };
    }

    private static PreviewState BuildReplayBeforeState(
        OperationalDraft draft,
        OperationalSimulationResult replay,
        PreviewState currentBaseState)
    {
        return draft.DraftType == ProjectionAdjustmentDraftType && replay.OperationalProjection is not null
            ? BuildCurrentBaseState(replay.OperationalProjection)
            : currentBaseState;
    }

    private static PreviewState BuildReplayAfterState(
        OperationalDraft draft,
        OperationalSimulationResult replay,
        PreviewState beforeState)
    {
        return draft.DraftType switch
        {
            WeeklyCorrectionDraftType or WeeklyClosingPreviewDraftType when replay.WeeklyCorrectionProposal is not null
                => BuildWeeklyCorrectionAfterState(beforeState, new WeeklyCorrectionApprovalPayload
                {
                    ExistingWeeklyClosingId = replay.WeeklyCorrectionProposal.ExistingWeeklyClosingId,
                    WeekStartDate = replay.WeeklyCorrectionProposal.WeekStartDate,
                    NeededBalls = replay.WeeklyCorrectionProposal.NeededBalls,
                    ProducedBalls = replay.WeeklyCorrectionProposal.ProducedBalls,
                    UsedBalls = replay.WeeklyCorrectionProposal.UsedBalls,
                    LostBalls = replay.WeeklyCorrectionProposal.LostBalls,
                    LeftoverReadyBalls = replay.WeeklyCorrectionProposal.LeftoverReadyBalls,
                    LeftoverAttentionBalls = replay.WeeklyCorrectionProposal.LeftoverAttentionBalls,
                    LeftoverMixedLoads = replay.WeeklyCorrectionProposal.LeftoverMixedLoads,
                    Notes = replay.WeeklyCorrectionProposal.Notes,
                    CorrectionReason = replay.WeeklyCorrectionProposal.Reason
                }),
            DoughTaskDraftType when replay.DoughTaskDraftProposal is not null
                => BuildDoughTaskAfterState(beforeState, new DoughTaskApprovalPayload
                {
                    TaskDate = replay.DoughTaskDraftProposal.TaskDate,
                    PrepItemId = replay.DoughTaskDraftProposal.PrepItemId,
                    PrepStationId = replay.DoughTaskDraftProposal.PrepStationId,
                    AssignedRole = replay.DoughTaskDraftProposal.AssignedRole,
                    TaskType = replay.DoughTaskDraftProposal.TaskType,
                    QuantityUnit = replay.DoughTaskDraftProposal.QuantityUnit,
                    QuantityValue = replay.DoughTaskDraftProposal.Quantity,
                    CompletionQuantityValue = replay.DoughTaskDraftProposal.CompletionQuantity,
                    Notes = replay.DoughTaskDraftProposal.Notes,
                    AutoCompleteOnApproval = replay.DoughTaskDraftProposal.AutoCompleteOnApproval
                }),
            DailyClosingDraftType when replay.DailyClosingDraftProposal is not null
                => BuildDailyClosingAfterState(beforeState, new DailyClosingApprovalPayload
                {
                    ExistingDailyClosingId = replay.DailyClosingDraftProposal.ExistingDailyClosingId,
                    ClosingDate = replay.DailyClosingDraftProposal.ClosingDate,
                    ForecastNeededBalls = replay.DailyClosingDraftProposal.ForecastNeededBalls,
                    ActualUsedBalls = replay.DailyClosingDraftProposal.ActualUsedBalls,
                    UsageBreakdown = replay.DailyClosingDraftProposal.UsageBreakdown,
                    Notes = replay.DailyClosingDraftProposal.Notes,
                    CorrectionNote = replay.DailyClosingDraftProposal.CorrectionNote
                }, ExtractIntProperty(replay.BeforeSnapshotJson, "ExistingActualUsedBalls")),
            RestaurantEventDraftType when replay.RestaurantEventDraftProposal is not null
                => BuildRestaurantEventAfterState(beforeState, new RestaurantEventApprovalPayload
                {
                    ExistingRestaurantEventId = replay.RestaurantEventDraftProposal.ExistingRestaurantEventId,
                    EventDate = replay.RestaurantEventDraftProposal.EventDate,
                    Name = replay.RestaurantEventDraftProposal.Name,
                    EstimatedPizzas = replay.RestaurantEventDraftProposal.EstimatedPizzas,
                    EstimatedDoughBalls = replay.RestaurantEventDraftProposal.EstimatedDoughBalls,
                    ExpectedPeopleMinimum = replay.RestaurantEventDraftProposal.ExpectedPeopleMinimum,
                    ExpectedPeopleMaximum = replay.RestaurantEventDraftProposal.ExpectedPeopleMaximum,
                    AllowShortFermentation = replay.RestaurantEventDraftProposal.AllowShortFermentation,
                    Notes = replay.RestaurantEventDraftProposal.Notes,
                    PreviousNarrativeDoughBalls = replay.RestaurantEventDraftProposal.PreviousNarrativeDoughBalls
                }, ExtractIntProperty(replay.BeforeSnapshotJson, "ExistingEstimatedDoughBalls")),
            ProjectionAdjustmentDraftType when replay.ProjectionAdjustmentDraftProposal is not null
                => BuildProjectionAdjustmentAfterState(beforeState, new ProjectionAdjustmentDraftPayload
                {
                    ReferenceDate = replay.ProjectionAdjustmentDraftProposal.ReferenceDate,
                    WeekStartDate = replay.ProjectionAdjustmentDraftProposal.WeekStartDate,
                    WeekEndDate = replay.ProjectionAdjustmentDraftProposal.WeekEndDate,
                    ReadyNowBalls = replay.ProjectionAdjustmentDraftProposal.ReadyNowBalls,
                    BallsReadyForService = replay.ProjectionAdjustmentDraftProposal.BallsReadyForService,
                    RemainingWeekDemandBalls = replay.ProjectionAdjustmentDraftProposal.RemainingWeekDemandBalls,
                    ProjectedCoverageBalls = replay.ProjectionAdjustmentDraftProposal.ProjectedCoverageBalls,
                    ProjectedShortageBalls = replay.ProjectionAdjustmentDraftProposal.ProjectedShortageBalls,
                    SuggestedAdditionalBallDoughBalls = replay.ProjectionAdjustmentDraftProposal.SuggestedAdditionalBallDoughBalls,
                    SuggestedAdditionalMakeDoughLoads = replay.ProjectionAdjustmentDraftProposal.SuggestedAdditionalMakeDoughLoads,
                    Notes = replay.ProjectionAdjustmentDraftProposal.Notes
                }),
            _ => BuildPersistedAfterState(draft, beforeState)
        };
    }

    private static PreviewState BuildWeeklyCorrectionAfterState(
        PreviewState beforeState,
        WeeklyCorrectionApprovalPayload payload)
    {
        return new PreviewState
        {
            ReadyNowBalls = beforeState.ReadyNowBalls,
            WeeklyUsedBalls = payload.UsedBalls,
            ProductionBalls = payload.ProducedBalls,
            ExternalEventConsumption = beforeState.ExternalEventConsumption,
            BallsReadyForService = payload.LeftoverReadyBalls + payload.LeftoverAttentionBalls
        };
    }

    private static PreviewState BuildDoughTaskAfterState(
        PreviewState beforeState,
        DoughTaskApprovalPayload payload)
    {
        var quantityBalls = NormalizeQuantityToBalls(payload.QuantityUnit, payload.QuantityValue);
        var completionBalls = NormalizeQuantityToBalls(
            payload.QuantityUnit,
            payload.CompletionQuantityValue ?? payload.QuantityValue);
        var isBallDough = string.Equals(payload.TaskType, "BallDough", StringComparison.OrdinalIgnoreCase);
        var inventoryImpactBalls = payload.AutoCompleteOnApproval && isBallDough
            ? completionBalls
            : 0;
        var productionImpactBalls = payload.AutoCompleteOnApproval
            ? completionBalls
            : 0;

        return new PreviewState
        {
            ReadyNowBalls = beforeState.ReadyNowBalls + inventoryImpactBalls,
            WeeklyUsedBalls = beforeState.WeeklyUsedBalls,
            ProductionBalls = beforeState.ProductionBalls + productionImpactBalls,
            ExternalEventConsumption = beforeState.ExternalEventConsumption,
            BallsReadyForService = beforeState.BallsReadyForService + inventoryImpactBalls
        };
    }

    private static PreviewState BuildDailyClosingAfterState(
        PreviewState beforeState,
        DailyClosingApprovalPayload payload,
        int? existingActualUsedBalls)
    {
        var eventUsageBalls = payload.UsageBreakdown
            .Where(component => string.Equals(component.Category, "Event", StringComparison.OrdinalIgnoreCase))
            .Sum(component => component.Balls);
        var priorDayUsage = Math.Max(existingActualUsedBalls ?? 0, 0);

        return new PreviewState
        {
            ReadyNowBalls = beforeState.ReadyNowBalls,
            WeeklyUsedBalls = beforeState.WeeklyUsedBalls - priorDayUsage + payload.ActualUsedBalls,
            ProductionBalls = beforeState.ProductionBalls,
            ExternalEventConsumption = beforeState.ExternalEventConsumption + eventUsageBalls,
            BallsReadyForService = beforeState.BallsReadyForService
        };
    }

    private static PreviewState BuildRestaurantEventAfterState(
        PreviewState beforeState,
        RestaurantEventApprovalPayload payload,
        int? existingEstimatedDoughBalls)
    {
        var previousEventBalls = Math.Max(existingEstimatedDoughBalls ?? 0, 0);

        return new PreviewState
        {
            ReadyNowBalls = beforeState.ReadyNowBalls,
            WeeklyUsedBalls = beforeState.WeeklyUsedBalls,
            ProductionBalls = beforeState.ProductionBalls,
            ExternalEventConsumption = Math.Max(beforeState.ExternalEventConsumption - previousEventBalls, 0) + payload.EstimatedDoughBalls,
            BallsReadyForService = beforeState.BallsReadyForService
        };
    }

    private static PreviewState BuildProjectionAdjustmentAfterState(
        PreviewState beforeState,
        ProjectionAdjustmentDraftPayload payload)
    {
        return new PreviewState
        {
            ReadyNowBalls = beforeState.ReadyNowBalls,
            WeeklyUsedBalls = beforeState.WeeklyUsedBalls,
            ProductionBalls = beforeState.ProductionBalls + payload.SuggestedAdditionalBallDoughBalls,
            ExternalEventConsumption = beforeState.ExternalEventConsumption,
            BallsReadyForService = beforeState.BallsReadyForService + payload.SuggestedAdditionalBallDoughBalls
        };
    }

    private static PreviewDiff BuildDiff(PreviewState before, PreviewState after)
    {
        return new PreviewDiff
        {
            ReadyNowDelta = after.ReadyNowBalls - before.ReadyNowBalls,
            WeeklyUsedDelta = after.WeeklyUsedBalls - before.WeeklyUsedBalls,
            ProductionDelta = after.ProductionBalls - before.ProductionBalls,
            ExternalEventConsumptionDelta = after.ExternalEventConsumption - before.ExternalEventConsumption,
            BallsReadyForServiceDelta = after.BallsReadyForService - before.BallsReadyForService,
            Changes =
            [
                $"ReadyNow: {before.ReadyNowBalls} -> {after.ReadyNowBalls}",
                $"WeeklyUsed: {before.WeeklyUsedBalls} -> {after.WeeklyUsedBalls}",
                $"Production: {before.ProductionBalls} -> {after.ProductionBalls}",
                $"ExternalEventConsumption: {before.ExternalEventConsumption} -> {after.ExternalEventConsumption}",
                $"BallsReadyForService: {before.BallsReadyForService} -> {after.BallsReadyForService}"
            ]
        };
    }

    private static bool HasConflicts(
        PreviewState afterState,
        IReadOnlyList<OperationalValidationWarning> warnings)
    {
        return afterState.ReadyNowBalls < 0 ||
               afterState.WeeklyUsedBalls < 0 ||
               afterState.ProductionBalls < 0 ||
               afterState.ExternalEventConsumption < 0 ||
               afterState.BallsReadyForService < 0 ||
               warnings.Any(warning => warning.BlocksDraft || warning.Code.Contains("conflict", StringComparison.OrdinalIgnoreCase));
    }

    private static string CalculateRiskLevel(
        IReadOnlyList<OperationalValidationWarning> warnings,
        bool hasConflicts)
    {
        if (hasConflicts || warnings.Any(warning => warning.BlocksDraft))
        {
            return "High";
        }

        if (warnings.Any(warning => warning.RequiresHumanReview) || warnings.Count > 2)
        {
            return "Medium";
        }

        return "Low";
    }

    private static IReadOnlyList<OperationalValidationWarning> DeserializeWarnings(string validationWarningsJson)
    {
        if (string.IsNullOrWhiteSpace(validationWarningsJson))
        {
            return Array.Empty<OperationalValidationWarning>();
        }

        return JsonSerializer.Deserialize<IReadOnlyList<OperationalValidationWarning>>(validationWarningsJson, JsonOptions)
               ?? Array.Empty<OperationalValidationWarning>();
    }

    private static IReadOnlyList<OperationalValidationWarning> MergeWarnings(
        IReadOnlyList<OperationalValidationWarning> primary,
        IReadOnlyList<OperationalValidationWarning> secondary)
    {
        return primary
            .Concat(secondary)
            .GroupBy(warning => $"{warning.Code}|{warning.Message}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    private static bool HasPersistedSnapshot(OperationalDraft draft)
    {
        return !string.IsNullOrWhiteSpace(draft.BeforeSnapshotJson) &&
               !string.IsNullOrWhiteSpace(draft.AfterPreviewJson);
    }

    private static bool HasStateDrift(PreviewState persistedBeforeState, PreviewState currentBaseState)
    {
        return persistedBeforeState.ReadyNowBalls != currentBaseState.ReadyNowBalls ||
               persistedBeforeState.WeeklyUsedBalls != currentBaseState.WeeklyUsedBalls ||
               persistedBeforeState.ProductionBalls != currentBaseState.ProductionBalls ||
               persistedBeforeState.ExternalEventConsumption != currentBaseState.ExternalEventConsumption ||
               persistedBeforeState.BallsReadyForService != currentBaseState.BallsReadyForService;
    }

    private static string FormatWarning(OperationalValidationWarning warning)
    {
        return $"{warning.Code}: {warning.Message}";
    }

    private static T DeserializePayload<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"The persisted draft payload for {typeof(T).Name} is invalid.");
    }

    private static int NormalizeQuantityToBalls(string quantityUnit, int quantityValue)
    {
        return string.Equals(quantityUnit, "FullLoads", StringComparison.OrdinalIgnoreCase)
            ? checked(quantityValue * DoughRules.StandardBatchBalls)
            : quantityValue;
    }

    private static int? ExtractIntProperty(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        if (!TryGetProperty(document.RootElement, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.String when int.TryParse(property.GetString(), out var stringValue) => stringValue,
            _ => null
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

    private readonly record struct DraftPreviewContext(
        DateOnly ReferenceDate,
        int HistoricalWeeksToUse);

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
}
