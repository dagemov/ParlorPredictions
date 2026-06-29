using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalDraftRepository
{
    Task AddAsync(OperationalDraft draft, CancellationToken cancellationToken = default);

    Task<OperationalDraft?> GetByIdAsync(Guid draftId, CancellationToken cancellationToken = default);

    Task<OperationalDraft?> GetLatestByCorrelationIdAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
