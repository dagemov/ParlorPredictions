using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface ISalesHistoryReadRepository
{
    Task<IReadOnlyCollection<SalesHistory>> GetRecentByDayOfWeekAsync(
        DateOnly targetDate,
        int historicalWeeksToUse,
        CancellationToken cancellationToken = default);
}
