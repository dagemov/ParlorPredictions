using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.Prep;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughCorrectionAdminService
{
    Task CorrectPrepTaskAsync(
        AdminCorrectPrepTaskRequest request,
        CancellationToken cancellationToken = default);

    Task CorrectDoughBatchAsync(
        AdminCorrectDoughBatchRequest request,
        CancellationToken cancellationToken = default);
}
