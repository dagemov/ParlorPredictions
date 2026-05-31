using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Repositories;

public sealed class ManagerPrepRecommendationRepository : IManagerPrepRecommendationRepository
{
    private readonly ParlorPredictionDbContext _dbContext;

    public ManagerPrepRecommendationRepository(ParlorPredictionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        ManagerPrepRecommendation recommendation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ManagerPrepRecommendations.AddAsync(recommendation, cancellationToken);
    }

    public Task<ManagerPrepRecommendation?> GetLatestByPrepItemOnOrBeforeDateAsync(
        Guid prepItemId,
        DateOnly recommendationDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ManagerPrepRecommendations
            .AsNoTracking()
            .Include(recommendation => recommendation.PrepItem)
            .Include(recommendation => recommendation.CreatedByUser)
            .Where(recommendation =>
                recommendation.PrepItemId == prepItemId &&
                recommendation.RecommendationDate <= recommendationDate)
            .OrderByDescending(recommendation => recommendation.RecommendationDate)
            .ThenByDescending(recommendation => recommendation.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerPrepRecommendation>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? prepItemId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ManagerPrepRecommendations
            .AsNoTracking()
            .Include(recommendation => recommendation.PrepItem)
            .Include(recommendation => recommendation.CreatedByUser)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(recommendation => recommendation.RecommendationDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(recommendation => recommendation.RecommendationDate <= toDate.Value);
        }

        if (prepItemId.HasValue && prepItemId.Value != Guid.Empty)
        {
            query = query.Where(recommendation => recommendation.PrepItemId == prepItemId.Value);
        }

        return await query
            .OrderByDescending(recommendation => recommendation.RecommendationDate)
            .ThenByDescending(recommendation => recommendation.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }
}
