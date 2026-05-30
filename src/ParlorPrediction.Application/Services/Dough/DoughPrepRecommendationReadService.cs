using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughPrepRecommendationReadService : IDoughPrepRecommendationReadService
{
    private readonly IDoughPrepRecommendationReadRepository _recommendationReadRepository;

    public DoughPrepRecommendationReadService(IDoughPrepRecommendationReadRepository recommendationReadRepository)
    {
        _recommendationReadRepository = recommendationReadRepository;
    }

    public async Task<DoughRecommendationDetailResponse?> GetLatestByDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var recommendation = await _recommendationReadRepository.GetLatestByDateAsync(targetDate, cancellationToken);
        return recommendation is null ? null : Map(recommendation);
    }

    private static DoughRecommendationDetailResponse Map(DoughPrepRecommendation recommendation)
    {
        return new DoughRecommendationDetailResponse
        {
            RecommendationId = recommendation.Id,
            RecommendationDate = recommendation.RecommendationDate,
            RequiredBalls = recommendation.RequiredBalls,
            HistoricalAverageBalls = recommendation.HistoricalAverageBalls,
            EventEstimatedBalls = recommendation.EventEstimatedBalls,
            AvailableBalls = recommendation.AvailableBalls,
            MissingBalls = recommendation.MissingBalls,
            RecommendedCases = recommendation.RecommendedCases,
            RecommendedLoads = recommendation.RecommendedLoads,
            ShouldMakeDough = recommendation.ShouldMakeDough,
            ShouldBallDough = recommendation.ShouldBallDough,
            UsesShortFermentationException = recommendation.UsesShortFermentationException,
            Reason = recommendation.Reason,
            CreatedAtUtc = recommendation.CreatedAtUtc
        };
    }
}
