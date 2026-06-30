using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class ConsumptionLedgerRepository : IConsumptionLedgerRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public ConsumptionLedgerRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ConsumptionLedger entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ConsumptionLedger>().AddAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<ConsumptionLedger>> ListByOccurredOnRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ConsumptionLedger>()
            .AsNoTracking()
            .Where(entry => entry.OccurredOn >= fromDate && entry.OccurredOn <= toDate)
            .OrderBy(entry => entry.OccurredOn)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
