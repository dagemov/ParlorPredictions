using ParlorPrediction.Mcp.Security;
using ParlorPrediction.Mcp.Tools;

namespace ParlorPrediction.Mcp;

public sealed class McpHost
{
    public McpHost(
        McpToolAllowlist toolAllowlist,
        InventoryTools inventoryTools,
        OperationalNarrativeTools operationalNarrativeTools,
        PlanningTools planningTools,
        WeeklyTools weeklyTools)
    {
        ToolAllowlist = toolAllowlist;
        InventoryTools = inventoryTools;
        OperationalNarrativeTools = operationalNarrativeTools;
        PlanningTools = planningTools;
        WeeklyTools = weeklyTools;
    }

    public McpToolAllowlist ToolAllowlist { get; }

    public InventoryTools InventoryTools { get; }

    public OperationalNarrativeTools OperationalNarrativeTools { get; }

    public PlanningTools PlanningTools { get; }

    public WeeklyTools WeeklyTools { get; }
}
