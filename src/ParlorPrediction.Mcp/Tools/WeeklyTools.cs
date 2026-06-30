using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Mcp.Contracts;
using ParlorPrediction.Mcp.Security;

namespace ParlorPrediction.Mcp.Tools;

public sealed class WeeklyTools
{
    private readonly IOperationalDraftService _operationalDraftService;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;
    private readonly McpToolAllowlist _toolAllowlist;

    public WeeklyTools(
        IOperationalDraftService operationalDraftService,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService,
        McpToolAllowlist toolAllowlist)
    {
        _operationalDraftService = operationalDraftService;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
        _toolAllowlist = toolAllowlist;
    }

    public async Task<ReadWeeklyClosingToolResponse> ReadWeeklyClosingAsync(
        ReadWeeklyClosingToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.ReadWeeklyClosing);

        var referenceDate = request.ReferenceDate ?? DateOnly.FromDateTime(DateTime.Today);
        var closings = await _weeklyDoughClosingReadService.GetWeeklyClosingsAsync(
            new GetWeeklyClosingsRequest
            {
                FromWeekStartDate = request.FromWeekStartDate,
                ToWeekStartDate = request.ToWeekStartDate
            },
            cancellationToken);
        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = referenceDate
            },
            cancellationToken);

        return new ReadWeeklyClosingToolResponse
        {
            Closings = closings,
            Carryover = carryover
        };
    }

    public async Task<OperationalDraftToolResponse> DraftWeeklyCorrectionAsync(
        DraftWeeklyCorrectionToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.DraftWeeklyCorrection);

        var draft = await _operationalDraftService.CreateWeeklyCorrectionDraftAsync(
            ToOperationalNarrativeRequest(request),
            cancellationToken);

        return new OperationalDraftToolResponse
        {
            Draft = draft.Draft,
            AuditEntry = draft.AuditEntry,
            DiffJson = draft.DiffJson
        };
    }

    public async Task<ValidateClosingBeforeSaveToolResponse> ValidateClosingBeforeSaveAsync(
        ValidateClosingBeforeSaveToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.ValidateClosingBeforeSave);

        var validation = await _operationalDraftService.ValidateClosingBeforeSaveAsync(
            ToOperationalNarrativeRequest(request),
            cancellationToken);

        return new ValidateClosingBeforeSaveToolResponse
        {
            IsValid = validation.IsValid,
            ValidationWarningsJson = validation.ValidationWarningsJson
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
