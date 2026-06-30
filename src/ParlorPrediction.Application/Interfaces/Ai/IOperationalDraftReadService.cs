using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalDraftReadService
{
    Task<IReadOnlyList<OperationalDraftInboxItem>> GetInboxAsync(
        int recentReviewedCount = 12,
        CancellationToken cancellationToken = default);

    Task<OperationalDraftDetailResult?> GetDetailAsync(
        Guid draftId,
        CancellationToken cancellationToken = default);
}

public sealed class OperationalDraftInboxItem
{
    public required OperationalDraft Draft { get; init; }

    public required OperationalPreviewResult Preview { get; init; }
}

public sealed class OperationalDraftDetailResult
{
    public required OperationalDraft Draft { get; init; }

    public required IReadOnlyList<OperationalAuditEntry> AuditEntries { get; init; }
}
