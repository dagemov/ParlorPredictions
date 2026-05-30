using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class DoughPrepRecommendationReadRepository : IDoughPrepRecommendationReadRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public DoughPrepRecommendationReadRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DoughPrepRecommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughPrepRecommendations
            .AsNoTracking()
            .FirstOrDefaultAsync(recommendation => recommendation.Id == id, cancellationToken);
    }
}
