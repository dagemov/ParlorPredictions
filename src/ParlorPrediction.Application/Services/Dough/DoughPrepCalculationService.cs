using System.Text;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughPrepCalculationService : IDoughPrepCalculationService
{
    private readonly IDoughInventoryReadRepository _doughInventoryReadRepository;
    private readonly IRestaurantEventReadRepository _restaurantEventReadRepository;
    private readonly ISalesHistoryReadRepository _salesHistoryReadRepository;

    public DoughPrepCalculationService(
        IDoughInventoryReadRepository doughInventoryReadRepository,
        IRestaurantEventReadRepository restaurantEventReadRepository,
        ISalesHistoryReadRepository salesHistoryReadRepository)
    {
        _doughInventoryReadRepository = doughInventoryReadRepository;
        _restaurantEventReadRepository = restaurantEventReadRepository;
        _salesHistoryReadRepository = salesHistoryReadRepository;
    }

    public async Task<DoughPrepCalculationResult> CalculateAsync(
        CalculateDoughPrepRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var historicalSales = await _salesHistoryReadRepository.GetRecentByDayOfWeekAsync(
            request.TargetDate,
            request.HistoricalWeeksToUse,
            cancellationToken);

        var events = await _restaurantEventReadRepository.GetByDateAsync(request.TargetDate, cancellationToken);
        var latestInventorySnapshot = await _doughInventoryReadRepository.GetLatestSnapshotOnOrBeforeAsync(
            request.TargetDate,
            cancellationToken);

        var historicalAverageBalls = CalculateHistoricalAverageBalls(historicalSales);
        var eventEstimatedBalls = events.Sum(restaurantEvent => restaurantEvent.EstimatedDoughBalls);
        var requiredBalls = checked(historicalAverageBalls + eventEstimatedBalls);
        var availableBalls = latestInventorySnapshot?.AvailableBalls ?? 0;
        var missingBalls = Math.Max(requiredBalls - availableBalls, 0);
        var recommendedCases = CalculateRoundedUpUnits(missingBalls, DoughRules.BallsPerCase);
        var recommendedLoads = CalculateRoundedUpUnits(recommendedCases, DoughRules.StandardBatchCases);
        var shouldMakeDough = missingBalls > 0;

        // TODO: refine this with unballed, ready-to-use DoughBatch data in a later phase.
        var shouldBallDough = missingBalls > 0;

        var usesShortFermentationException =
            events.Any(restaurantEvent => restaurantEvent.AllowShortFermentation) &&
            DoughRules.IsSummerEventMonth(request.TargetDate.Month);

        return new DoughPrepCalculationResult
        {
            TargetDate = request.TargetDate,
            RequiredBalls = requiredBalls,
            HistoricalAverageBalls = historicalAverageBalls,
            EventEstimatedBalls = eventEstimatedBalls,
            AvailableBalls = availableBalls,
            MissingBalls = missingBalls,
            RecommendedCases = recommendedCases,
            RecommendedLoads = recommendedLoads,
            ShouldMakeDough = shouldMakeDough,
            ShouldBallDough = shouldBallDough,
            UsesShortFermentationException = usesShortFermentationException,
            Reason = BuildReason(
                request,
                historicalSales,
                events,
                historicalAverageBalls,
                eventEstimatedBalls,
                availableBalls,
                missingBalls,
                recommendedCases,
                recommendedLoads,
                usesShortFermentationException)
        };
    }

    private static void ValidateRequest(CalculateDoughPrepRequest request)
    {
        if (request.TargetDate == default)
        {
            throw new ArgumentException("Target date is required.", nameof(request));
        }

        if (request.HistoricalWeeksToUse < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.HistoricalWeeksToUse), "Historical weeks to use must be at least 1.");
        }
    }

    private static int CalculateHistoricalAverageBalls(IReadOnlyCollection<SalesHistory> historicalSales)
    {
        if (historicalSales.Count == 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(historicalSales.Average(sale => sale.DoughBallsUsed));
    }

    private static int CalculateRoundedUpUnits(int totalUnitsNeeded, int unitsPerGroup)
    {
        if (totalUnitsNeeded <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(totalUnitsNeeded / (double)unitsPerGroup);
    }

    private static string BuildReason(
        CalculateDoughPrepRequest request,
        IReadOnlyCollection<SalesHistory> historicalSales,
        IReadOnlyCollection<RestaurantEvent> events,
        int historicalAverageBalls,
        int eventEstimatedBalls,
        int availableBalls,
        int missingBalls,
        int recommendedCases,
        int recommendedLoads,
        bool usesShortFermentationException)
    {
        var reasonBuilder = new StringBuilder();

        if (historicalSales.Count == 0)
        {
            reasonBuilder.Append(
                $"No historical {request.TargetDate.DayOfWeek} sales were found in the last {request.HistoricalWeeksToUse} weeks, so the historical baseline is 0 dough balls.");
        }
        else
        {
            reasonBuilder.Append(
                $"Based on the last {request.HistoricalWeeksToUse} weeks of {request.TargetDate.DayOfWeek} sales, the system expects {historicalAverageBalls} dough balls.");
        }

        reasonBuilder.Append($" Events add {eventEstimatedBalls} dough balls across {events.Count} event(s).");
        reasonBuilder.Append($" Current available dough is {availableBalls} balls, leaving a shortage of {missingBalls} balls.");

        if (recommendedCases > 0)
        {
            reasonBuilder.Append(
                $" Recommended production is {recommendedCases} case(s) across {recommendedLoads} load(s).");
        }
        else
        {
            reasonBuilder.Append(" Current inventory covers expected demand, so no additional dough production is recommended.");
        }

        if (usesShortFermentationException)
        {
            reasonBuilder.Append(" A short-fermentation exception may be considered because at least one summer event allows it.");
        }

        return reasonBuilder.ToString();
    }
}
