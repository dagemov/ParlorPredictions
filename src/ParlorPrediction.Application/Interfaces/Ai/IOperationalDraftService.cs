namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalDraftService
{
    Task<OperationalDraftEnvelope> CreateWeeklyCorrectionDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> CreateDoughTaskDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);

    Task<ClosingValidationResult> ValidateClosingBeforeSaveAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);
}
