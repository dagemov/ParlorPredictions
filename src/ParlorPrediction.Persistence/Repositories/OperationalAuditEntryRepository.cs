using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class OperationalAuditEntryRepository : IOperationalAuditEntryRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public OperationalAuditEntryRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(OperationalAuditEntry auditEntry, CancellationToken cancellationToken = default)
    {
        return _dbContext.OperationalAuditEntries.AddAsync(auditEntry, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<OperationalAuditEntry>> ListByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationalAuditEntries
            .AsNoTracking()
            .Where(entry => entry.CorrelationId == correlationId)
            .OrderBy(entry => entry.TimestampUtc)
            .ToArrayAsync(cancellationToken);
    }
}
