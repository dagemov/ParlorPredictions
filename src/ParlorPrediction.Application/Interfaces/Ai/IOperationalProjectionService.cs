namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalProjectionService
{
    Task<OperationalProjectionResult> ProjectAsync(
        OperationalProjectionRequest request,
        CancellationToken cancellationToken = default);
}
