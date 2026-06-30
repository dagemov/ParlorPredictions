using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Mcp.Contracts;
using ParlorPrediction.Mcp.Security;

namespace ParlorPrediction.Mcp.Tools;

public sealed class PlanningTools
{
    private readonly IOperationalDraftService _operationalDraftService;
    private readonly IOperationalPreviewService _operationalPreviewService;
    private readonly IOperationalSimulationService _operationalSimulationService;
    private readonly IOperationalWeeklyGoalExplanationService _operationalWeeklyGoalExplanationService;
    private readonly McpToolAllowlist _toolAllowlist;

    public PlanningTools(
        IOperationalDraftService operationalDraftService,
        IOperationalPreviewService operationalPreviewService,
        IOperationalSimulationService operationalSimulationService,
        IOperationalWeeklyGoalExplanationService operationalWeeklyGoalExplanationService,
        McpToolAllowlist toolAllowlist)
    {
        _operationalDraftService = operationalDraftService;
        _operationalPreviewService = operationalPreviewService;
        _operationalSimulationService = operationalSimulationService;
        _operationalWeeklyGoalExplanationService = operationalWeeklyGoalExplanationService;
        _toolAllowlist = toolAllowlist;
    }

    public async Task<ExplainWeeklyGoalToolResponse> ExplainWeeklyGoalAsync(
        ExplainWeeklyGoalToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.ExplainWeeklyGoal);

        var explanation = await _operationalWeeklyGoalExplanationService.ExplainAsync(
            request.ReferenceDate,
            request.HistoricalWeeksToUse,
            cancellationToken);

        return new ExplainWeeklyGoalToolResponse
        {
            Explanation = explanation.Explanation,
            WeeklyGoal = explanation.WeeklyGoal,
            Availability = explanation.Availability,
            InventoryImpact = explanation.InventoryImpact
        };
    }

    public async Task<OperationalPreviewResult> PreviewOperationalDraftAsync(
        PreviewOperationalDraftToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.PreviewOperationalDraft);

        return await _operationalPreviewService.BuildPreviewAsync(
            request.DraftId,
            cancellationToken);
    }

    public async Task<SimulateOperationalProjectionToolResponse> SimulateOperationalProjectionAsync(
        SimulateOperationalProjectionToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.SimulateOperationalProjection);

        var simulation = await _operationalSimulationService.SimulateOperationalProjectionAsync(
            ToOperationalProjectionRequest(request),
            cancellationToken);
        var projection = simulation.OperationalProjection
            ?? throw new InvalidOperationException("Projection simulation did not produce an operational projection result.");

        return new SimulateOperationalProjectionToolResponse
        {
            CorrelationId = simulation.CorrelationId,
            ReadyNowBalls = projection.ReadyNowBalls,
            BallsReadyForService = projection.BallsReadyForService,
            ProjectedShortageBalls = projection.ProjectedShortageBalls,
            WeeklyClosingUsageConsistent = projection.WeeklyClosingUsageConsistent,
            BeforeSnapshotJson = simulation.BeforeSnapshotJson,
            AfterPreviewJson = simulation.AfterPreviewJson,
            DiffJson = simulation.DiffJson,
            ValidationWarningsJson = simulation.ValidationWarningsJson
        };
    }

    public async Task<OperationalDraftToolResponse> DraftProjectionBasedAdjustmentAsync(
        DraftProjectionBasedAdjustmentToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.DraftProjectionBasedAdjustment);

        var draft = await _operationalDraftService.CreateProjectionAdjustmentDraftAsync(
            ToOperationalProjectionRequest(request),
            cancellationToken);

        return new OperationalDraftToolResponse
        {
            Draft = draft.Draft,
            AuditEntry = draft.AuditEntry,
            DiffJson = draft.DiffJson
        };
    }

    private static OperationalProjectionRequest ToOperationalProjectionRequest(
        SimulateOperationalProjectionToolRequest request)
    {
        return new OperationalProjectionRequest
        {
            CorrelationId = request.CorrelationId,
            ReferenceDate = request.ReferenceDate,
            HistoricalWeeksToUse = request.HistoricalWeeksToUse,
            Notes = request.Notes,
            ActorUserId = request.ActorUserId
        };
    }
}
