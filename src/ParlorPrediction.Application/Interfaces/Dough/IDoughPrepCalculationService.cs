using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughPrepCalculationService
{
    Task<DoughPrepCalculationResult> CalculateAsync(
        CalculateDoughPrepRequest request,
        CancellationToken cancellationToken = default);
}
