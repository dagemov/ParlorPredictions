using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Mcp.Contracts;
using ParlorPrediction.Mcp.Security;

namespace ParlorPrediction.Mcp.Tools;

public sealed class OperationalNarrativeTools
{
    private readonly IOperationalDraftService _operationalDraftService;
    private readonly IOperationalSimulationService _operationalSimulationService;
    private readonly McpToolAllowlist _toolAllowlist;

    public OperationalNarrativeTools(
        IOperationalDraftService operationalDraftService,
        IOperationalSimulationService operationalSimulationService,
        McpToolAllowlist toolAllowlist)
    {
        _operationalDraftService = operationalDraftService;
        _operationalSimulationService = operationalSimulationService;
        _toolAllowlist = toolAllowlist;
    }

    public async Task<SimulateOperationalNarrativeToolResponse> SimulateOperationalNarrativeAsync(
        SimulateOperationalNarrativeToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.SimulateOperationalNarrative);

        var simulation = await _operationalSimulationService.SimulateAsync(
            ToOperationalNarrativeRequest(request),
            cancellationToken);

        return new SimulateOperationalNarrativeToolResponse
        {
            CorrelationId = simulation.CorrelationId,
            IntentKind = simulation.Intent.Kind.ToString(),
            NormalizedSummary = simulation.NormalizedSummary,
            BeforeSnapshotJson = simulation.BeforeSnapshotJson,
            AfterPreviewJson = simulation.AfterPreviewJson,
            DiffJson = simulation.DiffJson,
            ValidationWarningsJson = simulation.ValidationWarningsJson
        };
    }

    public async Task<OperationalDraftToolResponse> DraftDoughTaskAsync(
        DraftDoughTaskToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.DraftDoughTask);

        var draft = await _operationalDraftService.CreateDoughTaskDraftAsync(
            ToOperationalNarrativeRequest(request),
            cancellationToken);

        return new OperationalDraftToolResponse
        {
            Draft = draft.Draft,
            AuditEntry = draft.AuditEntry,
            DiffJson = draft.DiffJson
        };
    }

    private static ParlorPrediction.Application.Interfaces.Ai.OperationalNarrativeRequest ToOperationalNarrativeRequest(
        SimulateOperationalNarrativeToolRequest request)
    {
        return new ParlorPrediction.Application.Interfaces.Ai.OperationalNarrativeRequest
        {
            CorrelationId = request.CorrelationId,
            SourceText = request.SourceText,
            ReferenceDate = request.ReferenceDate,
            TargetWeekStartDate = request.TargetWeekStartDate,
            HistoricalWeeksToUse = request.HistoricalWeeksToUse,
            ActorUserId = request.ActorUserId
        };
    }
}
