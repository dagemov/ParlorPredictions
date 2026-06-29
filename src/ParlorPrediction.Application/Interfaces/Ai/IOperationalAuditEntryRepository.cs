using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalAuditEntryRepository
{
    Task AddAsync(OperationalAuditEntry auditEntry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAuditEntry>> ListByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
