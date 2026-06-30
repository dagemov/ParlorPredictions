using ParlorPrediction.Application.Interfaces.Ai;

namespace ParlorPrediction.Application.Services.OperationalDrafts;

public sealed class OperationalDraftReadService : IOperationalDraftReadService
{
    private readonly IOperationalAuditEntryRepository _operationalAuditEntryRepository;
    private readonly IOperationalDraftRepository _operationalDraftRepository;
    private readonly IOperationalPreviewService _operationalPreviewService;

    public OperationalDraftReadService(
        IOperationalAuditEntryRepository operationalAuditEntryRepository,
        IOperationalDraftRepository operationalDraftRepository,
        IOperationalPreviewService operationalPreviewService)
    {
        _operationalAuditEntryRepository = operationalAuditEntryRepository;
        _operationalDraftRepository = operationalDraftRepository;
        _operationalPreviewService = operationalPreviewService;
    }

    public async Task<IReadOnlyList<OperationalDraftInboxItem>> GetInboxAsync(
        int recentReviewedCount = 12,
        CancellationToken cancellationToken = default)
    {
        var drafts = await _operationalDraftRepository.ListInboxAsync(recentReviewedCount, cancellationToken);
        var items = new List<OperationalDraftInboxItem>(drafts.Count);

        foreach (var draft in drafts)
        {
            var preview = await _operationalPreviewService.BuildPreviewAsync(draft.Id, cancellationToken);
            items.Add(new OperationalDraftInboxItem
            {
                Draft = draft,
                Preview = preview
            });
        }

        return items;
    }

    public async Task<OperationalDraftDetailResult?> GetDetailAsync(
        Guid draftId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _operationalDraftRepository.GetByIdAsync(draftId, cancellationToken);
        if (draft is null)
        {
            return null;
        }

        var auditEntries = await _operationalAuditEntryRepository.ListByCorrelationIdAsync(
            draft.CorrelationId,
            cancellationToken);

        return new OperationalDraftDetailResult
        {
            Draft = draft,
            AuditEntries = auditEntries
        };
    }
}
