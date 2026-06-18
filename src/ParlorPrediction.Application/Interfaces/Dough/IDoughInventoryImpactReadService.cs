using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughInventoryImpactReadService
{
    Task<DoughInventoryImpactResponse> GetInventoryImpactAsync(
        GetDoughInventoryImpactRequest request,
        CancellationToken cancellationToken = default);
}
