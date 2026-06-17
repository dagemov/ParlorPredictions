using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughBatchReadRepository : IDoughBatchReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughBatchReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<DoughBatch>> GetProducedOnOrBeforeAsync(
        DateOnly productionDate,
        CancellationToken cancellationToken = default)
    {
        return await DoughBatchSqlCompatibility.GetProducedOnOrBeforeAsync(
            _dbContext,
            productionDate,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<DoughBatch>> SearchForCorrectionAsync(
        DateOnly? batchDateFrom,
        DateOnly? batchDateTo,
        bool includeVoided,
        CancellationToken cancellationToken = default)
    {
        return await DoughBatchSqlCompatibility.SearchForCorrectionAsync(
            _dbContext,
            batchDateFrom,
            batchDateTo,
            includeVoided,
            cancellationToken);
    }
}
