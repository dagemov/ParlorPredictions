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

    public Task<DoughPrepRecommendation?> GetLatestByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DoughPrepRecommendations
            .AsNoTracking()
            .Where(recommendation => recommendation.RecommendationDate == targetDate)
            .OrderByDescending(recommendation => recommendation.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoughPrepRecommendation>> GetLatestBetweenDatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await _dbContext.DoughPrepRecommendations
            .AsNoTracking()
            .Where(recommendation =>
                recommendation.RecommendationDate >= startDate &&
                recommendation.RecommendationDate <= endDate)
            .OrderByDescending(recommendation => recommendation.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return recommendations
            .GroupBy(recommendation => recommendation.RecommendationDate)
            .Select(group => group.First())
            .OrderBy(recommendation => recommendation.RecommendationDate)
            .ToArray();
    }
}
