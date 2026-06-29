using System.Text.Json;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.OperationalDrafts;

public sealed class OperationalDraftService : IOperationalDraftService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOperationalSimulationService _operationalSimulationService;

    public OperationalDraftService(IOperationalSimulationService operationalSimulationService)
    {
        _operationalSimulationService = operationalSimulationService;
    }

    public async Task<OperationalDraftEnvelope> CreateWeeklyCorrectionDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var simulation = await _operationalSimulationService.SimulateAsync(request, cancellationToken);
        if (simulation.WeeklyCorrectionProposal is null)
        {
            throw new InvalidOperationException("The current narrative did not produce a weekly correction proposal.");
        }

        return CreateDraftEnvelope("WeeklyCorrection", request, simulation);
    }

    public async Task<OperationalDraftEnvelope> CreateDoughTaskDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var simulation = await _operationalSimulationService.SimulateAsync(request, cancellationToken);
        if (simulation.DoughTaskDraftProposal is null)
        {
            throw new InvalidOperationException("The current narrative did not produce a dough task draft proposal.");
        }

        return CreateDraftEnvelope("DoughTask", request, simulation);
    }

    public async Task<ClosingValidationResult> ValidateClosingBeforeSaveAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        var simulation = await _operationalSimulationService.SimulateAsync(request, cancellationToken);

        return new ClosingValidationResult
        {
            IsValid = simulation.ValidationWarnings.Count == 0,
            ValidationWarningsJson = simulation.ValidationWarningsJson,
            ValidationWarnings = simulation.ValidationWarnings
        };
    }

    private static OperationalDraftEnvelope CreateDraftEnvelope(
        string draftType,
        OperationalNarrativeRequest request,
        OperationalSimulationResult simulation)
    {
        var actorUserId = string.IsNullOrWhiteSpace(request.ActorUserId)
            ? "mcp-draft"
            : request.ActorUserId.Trim();
        var normalizedIntentJson = JsonSerializer.Serialize(simulation.Intent, simulation.Intent.GetType(), JsonOptions);
        var draft = OperationalDraft.Create(
            draftType,
            request.SourceText,
            normalizedIntentJson,
            simulation.BeforeSnapshotJson,
            simulation.AfterPreviewJson,
            simulation.ValidationWarningsJson,
            actorUserId);
        var auditEntry = OperationalAuditEntry.Create(
            simulation.CorrelationId,
            $"{draftType}DraftCreated",
            actorUserId,
            request.SourceText,
            normalizedIntentJson,
            simulation.BeforeSnapshotJson,
            simulation.AfterPreviewJson,
            simulation.ValidationWarningsJson,
            draft.Id);

        return new OperationalDraftEnvelope
        {
            Draft = draft,
            AuditEntry = auditEntry,
            DiffJson = simulation.DiffJson
        };
    }
}
