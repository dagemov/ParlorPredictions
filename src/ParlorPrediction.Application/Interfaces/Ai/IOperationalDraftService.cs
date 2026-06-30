namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalDraftService
{
    Task<OperationalDraftEnvelope> CreateWeeklyCorrectionDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> CreateDoughTaskDraftAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> CreateDoughTaskDraftAsync(
        OperationalDoughTaskDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> CreateDailyClosingDraftAsync(
        OperationalDailyClosingDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> CreateRestaurantEventDraftAsync(
        OperationalEventDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> CreateProjectionAdjustmentDraftAsync(
        OperationalProjectionRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> CreateWeeklyClosingPreviewDraftAsync(
        OperationalWeeklyClosingPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<ClosingValidationResult> ValidateClosingBeforeSaveAsync(
        OperationalNarrativeRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> MarkAsReadyForApprovalAsync(
        Guid draftId,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftApprovalResult> ApproveDraftAsync(
        Guid draftId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftEnvelope> RejectDraftAsync(
        Guid draftId,
        string reason,
        string? reviewedByUserId = null,
        CancellationToken cancellationToken = default);
}
