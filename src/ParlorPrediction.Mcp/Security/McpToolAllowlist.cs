namespace ParlorPrediction.Mcp.Security;

public sealed class McpToolAllowlist
{
    private static readonly HashSet<string> AllowedTools = new(StringComparer.Ordinal)
    {
        McpToolNames.ReadWeeklyClosing,
        McpToolNames.ReadDoughInventory,
        McpToolNames.ExplainWeeklyGoal,
        McpToolNames.SimulateOperationalNarrative,
        McpToolNames.DraftWeeklyCorrection,
        McpToolNames.DraftDoughTask,
        McpToolNames.ValidateClosingBeforeSave
    };

    public IReadOnlyCollection<string> GetRegisteredToolNames() => AllowedTools;

    public void EnsureAllowed(string toolName)
    {
        if (!AllowedTools.Contains(toolName))
        {
            throw new InvalidOperationException($"Tool '{toolName}' is not allowlisted for the v1 MCP.");
        }
    }
}
