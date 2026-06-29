using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Mcp.Contracts;
using ParlorPrediction.Mcp.Security;

namespace ParlorPrediction.Mcp.Tools;

public sealed class InventoryTools
{
    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;
    private readonly McpToolAllowlist _toolAllowlist;

    public InventoryTools(
        IDoughInventoryImpactReadService doughInventoryImpactReadService,
        McpToolAllowlist toolAllowlist)
    {
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
        _toolAllowlist = toolAllowlist;
    }

    public Task<DoughInventoryImpactResponse> ReadDoughInventoryAsync(
        ReadDoughInventoryToolRequest request,
        CancellationToken cancellationToken = default)
    {
        _toolAllowlist.EnsureAllowed(McpToolNames.ReadDoughInventory);

        return _doughInventoryImpactReadService.GetInventoryImpactAsync(
            new GetDoughInventoryImpactRequest
            {
                ReferenceDate = request.ReferenceDate,
                HistoricalWeeksToUse = request.HistoricalWeeksToUse
            },
            cancellationToken);
    }
}
