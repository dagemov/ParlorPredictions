namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalPreviewService
{
    Task<OperationalPreviewResult> BuildPreviewAsync(
        Guid draftId,
        CancellationToken cancellationToken = default);
}
