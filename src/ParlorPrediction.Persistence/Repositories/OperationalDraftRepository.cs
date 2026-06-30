using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class OperationalDraftRepository : IOperationalDraftRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public OperationalDraftRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(OperationalDraft draft, CancellationToken cancellationToken = default)
    {
        return _dbContext.OperationalDrafts.AddAsync(draft, cancellationToken).AsTask();
    }

    public Task<OperationalDraft?> GetByIdAsync(Guid draftId, CancellationToken cancellationToken = default)
    {
        return _dbContext.OperationalDrafts
            .FirstOrDefaultAsync(draft => draft.Id == draftId, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationalDraft>> ListInboxAsync(
        int recentReviewedCount,
        CancellationToken cancellationToken = default)
    {
        var safeRecentReviewedCount = recentReviewedCount < 1
            ? 12
            : recentReviewedCount;

        var pendingDrafts = await _dbContext.OperationalDrafts
            .AsNoTracking()
            .Where(draft =>
                draft.Status == OperationalDraftStatus.Pending ||
                draft.Status == OperationalDraftStatus.ReadyForApproval)
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        var recentReviewedDrafts = await _dbContext.OperationalDrafts
            .AsNoTracking()
            .Where(draft =>
                draft.Status == OperationalDraftStatus.Approved ||
                draft.Status == OperationalDraftStatus.Rejected)
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .Take(safeRecentReviewedCount)
            .ToArrayAsync(cancellationToken);

        return pendingDrafts
            .Concat(recentReviewedDrafts)
            .ToArray();
    }

    public Task<OperationalDraft?> GetLatestByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.OperationalDrafts
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .FirstOrDefaultAsync(draft => draft.CorrelationId == correlationId, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationalDraft>> ListByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationalDrafts
            .Where(draft => draft.CorrelationId == correlationId)
            .OrderBy(draft => draft.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
