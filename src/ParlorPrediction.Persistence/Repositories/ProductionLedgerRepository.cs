using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class ProductionLedgerRepository : IProductionLedgerRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public ProductionLedgerRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductionLedger entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ProductionLedger>().AddAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionLedger>> ListByOccurredOnRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ProductionLedger>()
            .AsNoTracking()
            .Where(entry => entry.OccurredOn >= fromDate && entry.OccurredOn <= toDate)
            .OrderBy(entry => entry.OccurredOn)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
