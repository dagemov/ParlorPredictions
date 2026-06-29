using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Entities;

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

    public Task<OperationalDraft?> GetLatestByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.OperationalDrafts
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .FirstOrDefaultAsync(draft => draft.CorrelationId == correlationId, cancellationToken);
    }
}
