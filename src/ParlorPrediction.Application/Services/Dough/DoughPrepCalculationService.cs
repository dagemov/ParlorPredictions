using System.Text;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughPrepCalculationService : IDoughPrepCalculationService
{
    private readonly IDoughDemandPlanReadRepository _doughDemandPlanReadRepository;
    private readonly IDoughInventoryReadRepository _doughInventoryReadRepository;
    private readonly IRestaurantEventReadRepository _restaurantEventReadRepository;
    private readonly ISalesHistoryReadRepository _salesHistoryReadRepository;

    public DoughPrepCalculationService(
        IDoughDemandPlanReadRepository doughDemandPlanReadRepository,
        IDoughInventoryReadRepository doughInventoryReadRepository,
        IRestaurantEventReadRepository restaurantEventReadRepository,
        ISalesHistoryReadRepository salesHistoryReadRepository)
    {
        _doughDemandPlanReadRepository = doughDemandPlanReadRepository;
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
        var demandPlans = await _doughDemandPlanReadRepository.GetActiveByDayOfWeekAsync(
            request.TargetDate.DayOfWeek,
            cancellationToken);

        var events = await _restaurantEventReadRepository.GetByDateAsync(request.TargetDate, cancellationToken);
        var latestInventorySnapshot = await _doughInventoryReadRepository.GetLatestSnapshotOnOrBeforeAsync(
            request.TargetDate,
            cancellationToken);

        var historicalAverageBalls = demandPlans.Count > 0
            ? CalculateDemandPlanBaselineBalls(demandPlans)
            : CalculateHistoricalAverageBalls(historicalSales);
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
                demandPlans,
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

    private static int CalculateDemandPlanBaselineBalls(IReadOnlyCollection<DoughDemandPlan> demandPlans)
    {
        if (demandPlans.Count == 0)
        {
            return 0;
        }

        return demandPlans.Sum(plan => plan.GetBaselineDoughBalls());
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
        IReadOnlyCollection<DoughDemandPlan> demandPlans,
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

        if (demandPlans.Count > 0)
        {
            reasonBuilder.Append(
                $"For {request.TargetDate.DayOfWeek}, the restaurant usually needs about {historicalAverageBalls} dough balls based on {demandPlans.Count} active planning entr{(demandPlans.Count == 1 ? "y" : "ies")}.");
        }
        else if (historicalSales.Count == 0)
        {
            reasonBuilder.Append(
                $"There is no recent sales history for {request.TargetDate.DayOfWeek} in the last {request.HistoricalWeeksToUse} weeks, so the usual dough needed for that day is starting from 0.");
        }
        else
        {
            reasonBuilder.Append(
                $"Looking at the last {request.HistoricalWeeksToUse} {request.TargetDate.DayOfWeek} service day{(request.HistoricalWeeksToUse == 1 ? string.Empty : "s")}, the restaurant usually needs about {historicalAverageBalls} dough balls.");
        }

        if (eventEstimatedBalls > 0)
        {
            reasonBuilder.Append(
                $" Scheduled event demand adds another {eventEstimatedBalls} dough balls across {events.Count} event{(events.Count == 1 ? string.Empty : "s")}.");
        }
        else
        {
            reasonBuilder.Append(" There is no extra event dough planned for this day.");
        }

        reasonBuilder.Append($" You currently have {availableBalls} dough balls available, so you are short by {missingBalls}.");

        if (recommendedCases > 0)
        {
            reasonBuilder.Append(
                $" That means the kitchen should plan for about {recommendedCases} case{(recommendedCases == 1 ? string.Empty : "s")} of dough, which rounds to {recommendedLoads} full batch{(recommendedLoads == 1 ? string.Empty : "es")}.");

            if (recommendedLoads == 1 && missingBalls < DoughRules.StandardBatchBalls)
            {
                reasonBuilder.Append(" One full batch will cover today's shortage and leave extra dough moving into the next prep day.");
            }
        }
        else
        {
            reasonBuilder.Append(" Current dough available already covers the day, so no new mixing is needed right now.");
        }

        if (usesShortFermentationException)
        {
            reasonBuilder.Append(" A shorter 1 to 2 day fermentation window may be considered because at least one summer event allows it.");
        }

        return reasonBuilder.ToString();
    }
}
