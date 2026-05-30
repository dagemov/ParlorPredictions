using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class SalesHistoryReadRepository : ISalesHistoryReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public SalesHistoryReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<SalesHistory>> GetRecentByDayOfWeekAsync(
        DateOnly targetDate,
        int historicalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var earliestDate = targetDate.AddDays(-(historicalWeeksToUse * 7));
        var targetDayOfWeek = targetDate.DayOfWeek;

        return await _dbContext.SalesHistories
            .AsNoTracking()
            .Where(sale =>
                sale.SaleDate < targetDate &&
                sale.SaleDate >= earliestDate &&
                sale.DayOfWeek == targetDayOfWeek)
            .OrderByDescending(sale => sale.SaleDate)
            .ThenBy(sale => sale.ProductName)
            .ToListAsync(cancellationToken);
    }
}
