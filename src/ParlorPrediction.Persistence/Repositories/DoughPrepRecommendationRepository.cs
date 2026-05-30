using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughPrepRecommendationRepository : IDoughPrepRecommendationRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughPrepRecommendationRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoughPrepRecommendation recommendation, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoughPrepRecommendations.AddAsync(recommendation, cancellationToken);
    }
}
