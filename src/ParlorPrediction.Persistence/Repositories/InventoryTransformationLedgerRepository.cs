using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class InventoryTransformationLedgerRepository : IInventoryTransformationLedgerRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public InventoryTransformationLedgerRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(InventoryTransformationLedger entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<InventoryTransformationLedger>().AddAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryTransformationLedger>> ListByOccurredOnRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<InventoryTransformationLedger>()
            .AsNoTracking()
            .Where(entry => entry.OccurredOn >= fromDate && entry.OccurredOn <= toDate)
            .OrderBy(entry => entry.OccurredOn)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
