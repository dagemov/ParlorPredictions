namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalSimulationService
{
    Task<OperationalSimulationResult> SimulateAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);
}
