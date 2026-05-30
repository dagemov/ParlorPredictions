using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughProductionPlanningService
{
    Task<DoughProductionPlanningResponse> PlanAsync(
        DoughProductionPlanningRequest request,
        CancellationToken cancellationToken = default);
}
