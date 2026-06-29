using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Mcp.Contracts;
using ParlorPrediction.Mcp.Security;

namespace ParlorPrediction.Mcp.Tools;

public sealed class PlanningTools
{
    private readonly IOperationalWeeklyGoalExplanationService _operationalWeeklyGoalExplanationService;
    private readonly McpToolAllowlist _toolAllowlist;

    public PlanningTools(
        IOperationalWeeklyGoalExplanationService operationalWeeklyGoalExplanationService,
        McpToolAllowlist toolAllowlist)
    {
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
}
