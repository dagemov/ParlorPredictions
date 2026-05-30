using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughPrepRecommendationService : IDoughPrepRecommendationService
{
    private readonly IDoughPrepCalculationService _doughPrepCalculationService;
    private readonly IDoughPrepRecommendationRepository _doughPrepRecommendationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DoughPrepRecommendationService(
        IDoughPrepCalculationService doughPrepCalculationService,
        IDoughPrepRecommendationRepository doughPrepRecommendationRepository,
        IUnitOfWork unitOfWork)
    {
        _doughPrepCalculationService = doughPrepCalculationService;
        _doughPrepRecommendationRepository = doughPrepRecommendationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GenerateDoughPrepRecommendationResponse> GenerateAsync(
        GenerateDoughPrepRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TargetDate == default)
        {
            throw new ArgumentException("Target date is required.", nameof(request));
        }

        if (request.HistoricalWeeksToUse < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.HistoricalWeeksToUse),
                "Historical weeks to use must be at least 1.");
        }

        var calculation = await _doughPrepCalculationService.CalculateAsync(
            new CalculateDoughPrepRequest
            {
                TargetDate = request.TargetDate,
                HistoricalWeeksToUse = request.HistoricalWeeksToUse
            },
            cancellationToken);

        var recommendation = DoughPrepRecommendation.FromCalculationSnapshot(
            calculation.TargetDate,
            calculation.RequiredBalls,
            calculation.HistoricalAverageBalls,
            calculation.EventEstimatedBalls,
            calculation.AvailableBalls,
            calculation.MissingBalls,
            calculation.RecommendedCases,
            calculation.RecommendedLoads,
            calculation.ShouldMakeDough,
            calculation.ShouldBallDough,
            calculation.UsesShortFermentationException,
            calculation.Reason);

        await _doughPrepRecommendationRepository.AddAsync(recommendation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GenerateDoughPrepRecommendationResponse
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
            Reason = recommendation.Reason
        };
    }
}
