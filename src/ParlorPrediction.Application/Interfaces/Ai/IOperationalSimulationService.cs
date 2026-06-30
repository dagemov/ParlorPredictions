namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalSimulationService
{
    Task<OperationalSimulationResult> SimulateAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalSimulationResult> SimulateDoughTaskAsync(
        OperationalDoughTaskDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalSimulationResult> SimulateDailyClosingAsync(
        OperationalDailyClosingDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalSimulationResult> SimulateRestaurantEventAsync(
        OperationalEventDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalSimulationResult> SimulateOperationalProjectionAsync(
        OperationalProjectionRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalSimulationResult> SimulateWeeklyClosingPreviewAsync(
        OperationalWeeklyClosingPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalSimulationResult> ReplayDraftAsync(
        ParlorPrediction.Domain.Entities.OperationalDraft draft,
        CancellationToken cancellationToken = default);
}
