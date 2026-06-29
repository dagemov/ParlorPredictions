using System.Text.Json;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.OperationalDrafts;

public sealed class OperationalDraftService : IOperationalDraftService
{
    private const string WeeklyCorrectionDraftType = "WeeklyCorrection";
    private const string DoughTaskDraftType = "DoughTask";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOperationalAuditEntryRepository _operationalAuditEntryRepository;
    private readonly IOperationalDraftRepository _operationalDraftRepository;
    private readonly IOperationalSimulationService _operationalSimulationService;
    private readonly IPrepTaskService _prepTaskService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWeeklyDoughClosingManagementService _weeklyDoughClosingManagementService;

    public OperationalDraftService(
        IOperationalAuditEntryRepository operationalAuditEntryRepository,
        IOperationalDraftRepository operationalDraftRepository,
        IOperationalSimulationService operationalSimulationService,
        IPrepTaskService prepTaskService,
        IUnitOfWork unitOfWork,
        IWeeklyDoughClosingManagementService weeklyDoughClosingManagementService)
    {
        _operationalAuditEntryRepository = operationalAuditEntryRepository;
        _operationalDraftRepository = operationalDraftRepository;
        _operationalSimulationService = operationalSimulationService;
        _prepTaskService = prepTaskService;
        _unitOfWork = unitOfWork;
        _weeklyDoughClosingManagementService = weeklyDoughClosingManagementService;
    }

    public Task<OperationalDraftEnvelope> CreateWeeklyCorrectionDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        return CreateDraftAsync(
            request,
            WeeklyCorrectionDraftType,
            simulation =>
            {
                var proposal = simulation.WeeklyCorrectionProposal
                    ?? throw new InvalidOperationException("The current narrative did not produce a weekly correction proposal.");

                return new WeeklyCorrectionApprovalPayload
                {
                    ExistingWeeklyClosingId = proposal.ExistingWeeklyClosingId,
                    WeekStartDate = proposal.WeekStartDate,
                    NeededBalls = proposal.NeededBalls,
                    ProducedBalls = proposal.ProducedBalls,
                    UsedBalls = proposal.UsedBalls,
                    LostBalls = proposal.LostBalls,
                    LeftoverReadyBalls = proposal.LeftoverReadyBalls,
                    LeftoverAttentionBalls = proposal.LeftoverAttentionBalls,
                    LeftoverMixedLoads = proposal.LeftoverMixedLoads,
                    Notes = proposal.Notes,
                    CorrectionReason = proposal.Reason
                };
            },
            cancellationToken);
    }

    public Task<OperationalDraftEnvelope> CreateDoughTaskDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        return CreateDraftAsync(
            request,
            DoughTaskDraftType,
            simulation =>
            {
                var proposal = simulation.DoughTaskDraftProposal
                    ?? throw new InvalidOperationException("The current narrative did not produce a dough task draft proposal.");

                return new DoughTaskApprovalPayload
                {
                    TaskDate = proposal.TaskDate,
                    PrepItemId = proposal.PrepItemId,
                    PrepStationId = proposal.PrepStationId,
                    AssignedRole = proposal.AssignedRole,
                    TaskType = proposal.TaskType,
                    QuantityUnit = proposal.QuantityUnit,
                    QuantityValue = proposal.Quantity,
                    CompletionQuantityValue = proposal.CompletionQuantity,
                    Notes = proposal.Notes,
                    AutoCompleteOnApproval = proposal.AutoCompleteOnApproval
                };
            },
            cancellationToken);
    }

    public async Task<ClosingValidationResult> ValidateClosingBeforeSaveAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var simulation = await _operationalSimulationService.SimulateAsync(request, cancellationToken);

        return new ClosingValidationResult
        {
            IsValid = !HasBlockingWarnings(simulation.ValidationWarnings),
            ValidationWarningsJson = simulation.ValidationWarningsJson,
            ValidationWarnings = simulation.ValidationWarnings
        };
    }

    public async Task<OperationalDraftEnvelope> MarkAsReadyForApprovalAsync(
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        var draft = await GetRequiredDraftAsync(draftId, cancellationToken);
        draft.MarkAsReadyForApproval();

        var auditEntry = CreateAuditEntry(
            draft,
            "OperationalDraftReadyForApproval",
            draft.CreatedBy);

        await _operationalAuditEntryRepository.AddAsync(auditEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OperationalDraftEnvelope
        {
            Draft = draft,
            AuditEntry = auditEntry
        };
    }

    public async Task<OperationalDraftApprovalResult> ApproveDraftAsync(
        Guid draftId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var draft = await GetRequiredDraftAsync(draftId, cancellationToken);
        EnsureDraftCanBeApproved(draft);

        Guid? approvedEntityId = null;
        OperationalAuditEntry auditEntry;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            approvedEntityId = await ApplyApprovalAsync(draft, userId.Trim(), cancellationToken);
            draft.Approve(userId, approvedEntityId);

            auditEntry = CreateAuditEntry(
                draft,
                "OperationalDraftApproved",
                userId,
                approvedEntityId);

            await _operationalAuditEntryRepository.AddAsync(auditEntry, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new OperationalDraftApprovalResult
        {
            Draft = draft,
            AuditEntry = auditEntry,
            ApprovedEntityId = approvedEntityId
        };
    }

    public async Task<OperationalDraftEnvelope> RejectDraftAsync(
        Guid draftId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var draft = await GetRequiredDraftAsync(draftId, cancellationToken);
        draft.Reject(reason);

        var auditEntry = CreateAuditEntry(
            draft,
            "OperationalDraftRejected",
            draft.CreatedBy);

        await _operationalAuditEntryRepository.AddAsync(auditEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OperationalDraftEnvelope
        {
            Draft = draft,
            AuditEntry = auditEntry
        };
    }

    private async Task<OperationalDraftEnvelope> CreateDraftAsync(
        OperationalNarrativeRequest request,
        string draftType,
        Func<OperationalSimulationResult, object> payloadFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var simulation = await _operationalSimulationService.SimulateAsync(request, cancellationToken);
        var actorUserId = ResolveActorUserId(request.ActorUserId);

        if (HasBlockingWarnings(simulation.ValidationWarnings))
        {
            var blockedAuditEntry = OperationalAuditEntry.Create(
                simulation.CorrelationId,
                $"{draftType}DraftBlocked",
                actorUserId,
                request.SourceText,
                SerializeIntent(simulation.Intent),
                simulation.BeforeSnapshotJson,
                simulation.AfterPreviewJson,
                simulation.ValidationWarningsJson,
                draftId: null);

            await _operationalAuditEntryRepository.AddAsync(blockedAuditEntry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException("The current narrative produced blocking validation warnings, so no draft was persisted.");
        }

        var draftPayloadJson = JsonSerializer.Serialize(payloadFactory(simulation), JsonOptions);
        var draft = OperationalDraft.Create(
            simulation.CorrelationId,
            draftType,
            request.SourceText,
            SerializeIntent(simulation.Intent),
            simulation.BeforeSnapshotJson,
            simulation.AfterPreviewJson,
            simulation.ValidationWarningsJson,
            draftPayloadJson,
            actorUserId);
        var auditEntry = CreateAuditEntry(
            draft,
            $"{draftType}DraftCreated",
            actorUserId);

        await _operationalDraftRepository.AddAsync(draft, cancellationToken);
        await _operationalAuditEntryRepository.AddAsync(auditEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OperationalDraftEnvelope
        {
            Draft = draft,
            AuditEntry = auditEntry,
            DiffJson = simulation.DiffJson
        };
    }

    private async Task<Guid?> ApplyApprovalAsync(
        OperationalDraft draft,
        string userId,
        CancellationToken cancellationToken)
    {
        return draft.DraftType switch
        {
            WeeklyCorrectionDraftType => await ApplyWeeklyCorrectionApprovalAsync(draft, userId, cancellationToken),
            DoughTaskDraftType => await ApplyDoughTaskApprovalAsync(draft, userId, cancellationToken),
            _ => throw new InvalidOperationException($"The draft type '{draft.DraftType}' is not supported by the MVP approval flow.")
        };
    }

    private async Task<Guid?> ApplyWeeklyCorrectionApprovalAsync(
        OperationalDraft draft,
        string userId,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<WeeklyCorrectionApprovalPayload>(draft.DraftPayloadJson);

        if (payload.ExistingWeeklyClosingId.HasValue)
        {
            var corrected = await _weeklyDoughClosingManagementService.CorrectWeeklyClosingAsync(
                new CorrectWeeklyDoughClosingRequest
                {
                    WeeklyDoughClosingId = payload.ExistingWeeklyClosingId.Value,
                    NeededBalls = payload.NeededBalls,
                    ProducedBalls = payload.ProducedBalls,
                    UsedBalls = payload.UsedBalls,
                    LostBalls = payload.LostBalls,
                    LeftoverReadyBalls = payload.LeftoverReadyBalls,
                    LeftoverAttentionBalls = payload.LeftoverAttentionBalls,
                    LeftoverMixedLoads = payload.LeftoverMixedLoads,
                    Notes = payload.Notes,
                    CorrectedByUserId = userId,
                    CorrectedAtUtc = DateTime.UtcNow,
                    CorrectionNote = payload.CorrectionReason
                },
                cancellationToken);

            return corrected.Id;
        }

        var created = await _weeklyDoughClosingManagementService.CreateWeeklyClosingAsync(
            new CreateWeeklyDoughClosingRequest
            {
                WeekStartDate = payload.WeekStartDate,
                NeededBalls = payload.NeededBalls,
                ProducedBalls = payload.ProducedBalls,
                UsedBalls = payload.UsedBalls,
                LostBalls = payload.LostBalls,
                LeftoverReadyBalls = payload.LeftoverReadyBalls,
                LeftoverAttentionBalls = payload.LeftoverAttentionBalls,
                LeftoverMixedLoads = payload.LeftoverMixedLoads,
                Notes = payload.Notes,
                ClosedByUserId = userId,
                ClosedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        return created.Id;
    }

    private async Task<Guid?> ApplyDoughTaskApprovalAsync(
        OperationalDraft draft,
        string userId,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<DoughTaskApprovalPayload>(draft.DraftPayloadJson);
        var createdTask = await _prepTaskService.CreateManualAsync(
            new SavePrepTaskRequest
            {
                TaskDate = payload.TaskDate,
                PrepItemId = payload.PrepItemId,
                PrepStationId = payload.PrepStationId,
                AssignedRole = payload.AssignedRole,
                TaskType = payload.TaskType,
                QuantityUnit = payload.QuantityUnit,
                QuantityValue = payload.QuantityValue,
                Notes = payload.Notes
            },
            cancellationToken);

        if (payload.AutoCompleteOnApproval)
        {
            await _prepTaskService.CompleteAsync(
                new CompletePrepTaskRequest
                {
                    PrepTaskId = createdTask.PrepTaskId,
                    CompletedByUserId = userId,
                    QuantityUnit = payload.QuantityUnit,
                    QuantityValue = payload.CompletionQuantityValue ?? payload.QuantityValue,
                    Notes = payload.Notes
                },
                cancellationToken);
        }

        return createdTask.PrepTaskId;
    }

    private async Task<OperationalDraft> GetRequiredDraftAsync(Guid draftId, CancellationToken cancellationToken)
    {
        if (draftId == Guid.Empty)
        {
            throw new ArgumentException("Draft id is required.", nameof(draftId));
        }

        return await _operationalDraftRepository.GetByIdAsync(draftId, cancellationToken)
            ?? throw new KeyNotFoundException("The operational draft could not be found.");
    }

    private static void EnsureDraftCanBeApproved(OperationalDraft draft)
    {
        if (draft.Status is not OperationalDraftStatus.Pending and not OperationalDraftStatus.ReadyForApproval)
        {
            throw new InvalidOperationException("Only pending drafts can move through approval.");
        }

        var warnings = DeserializeWarnings(draft.ValidationWarningsJson);
        if (HasBlockingWarnings(warnings))
        {
            throw new InvalidOperationException("Drafts with blocking validation warnings cannot be approved.");
        }
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

    private static bool HasBlockingWarnings(IReadOnlyList<OperationalValidationWarning> warnings)
    {
        return warnings.Any(warning => warning.BlocksDraft);
    }

    private static OperationalAuditEntry CreateAuditEntry(
        OperationalDraft draft,
        string actionType,
        string actorUserId,
        Guid? approvedEntityId = null)
    {
        return OperationalAuditEntry.Create(
            draft.CorrelationId,
            actionType,
            actorUserId,
            draft.SourceText,
            draft.NormalizedIntentJson,
            draft.BeforeSnapshotJson,
            draft.AfterPreviewJson,
            draft.ValidationWarningsJson,
            draft.Id,
            approvedEntityId);
    }

    private static T DeserializePayload<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"The persisted draft payload for {typeof(T).Name} is invalid.");
    }

    private static string ResolveActorUserId(string? actorUserId)
    {
        return string.IsNullOrWhiteSpace(actorUserId)
            ? "mcp-draft"
            : actorUserId.Trim();
    }

    private static string SerializeIntent(OperationalIntent intent)
    {
        return JsonSerializer.Serialize(intent, intent.GetType(), JsonOptions);
    }
}
