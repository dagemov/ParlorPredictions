using System.Text;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughProductionPlanningService : IDoughProductionPlanningService
{
    private readonly IDoughBatchReadRepository _doughBatchReadRepository;
    private readonly IDoughDemandPlanReadRepository _doughDemandPlanReadRepository;
    private readonly IDoughInventoryReadRepository _doughInventoryReadRepository;
    private readonly IRestaurantEventReadRepository _restaurantEventReadRepository;
    private readonly ISalesHistoryReadRepository _salesHistoryReadRepository;

    public DoughProductionPlanningService(
        IDoughBatchReadRepository doughBatchReadRepository,
        IDoughDemandPlanReadRepository doughDemandPlanReadRepository,
        IDoughInventoryReadRepository doughInventoryReadRepository,
        IRestaurantEventReadRepository restaurantEventReadRepository,
        ISalesHistoryReadRepository salesHistoryReadRepository)
    {
        _doughBatchReadRepository = doughBatchReadRepository;
        _doughDemandPlanReadRepository = doughDemandPlanReadRepository;
        _doughInventoryReadRepository = doughInventoryReadRepository;
        _restaurantEventReadRepository = restaurantEventReadRepository;
        _salesHistoryReadRepository = salesHistoryReadRepository;
    }

    public async Task<DoughProductionPlanningResponse> PlanAsync(
        DoughProductionPlanningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var horizonEndDate = request.ProductionDate.AddDays(request.DaysAhead - 1);
        var latestInventorySnapshot = await _doughInventoryReadRepository.GetLatestSnapshotOnOrBeforeAsync(
            request.ProductionDate,
            cancellationToken);
        var doughBatches = await _doughBatchReadRepository.GetProducedOnOrBeforeAsync(
            request.ProductionDate,
            cancellationToken);
        var upcomingEvents = await _restaurantEventReadRepository.GetBetweenDatesAsync(
            request.ProductionDate,
            horizonEndDate,
            cancellationToken);

        var upcomingNeeds = new List<DoughNeedByDateResponse>(request.DaysAhead);

        for (var offset = 0; offset < request.DaysAhead; offset++)
        {
            var needDate = request.ProductionDate.AddDays(offset);
            upcomingNeeds.Add(await BuildNeedByDateAsync(needDate, upcomingEvents, cancellationToken));
        }

        var upcomingNeedsReadOnly = upcomingNeeds
            .OrderBy(need => need.NeedDate)
            .ToArray();

        var needsForTodayWindow = upcomingNeedsReadOnly
            .Where(need => need.RecommendedMakeDate == request.ProductionDate)
            .OrderBy(need => need.NeedDate)
            .ToArray();

        var totalFutureRequiredBalls = upcomingNeedsReadOnly.Sum(need => need.TotalRequiredBalls);
        var readyBalls = latestInventorySnapshot?.AvailableBalls ?? 0;

        var fermentingBalls = doughBatches
            .Where(batch => batch.FermentationReadyDate > request.ProductionDate)
            .Sum(batch => batch.TotalBalls);

        var unballedBalls = doughBatches
            .Where(batch => !batch.IsBalled)
            .Sum(batch => batch.TotalBalls);

        var totalBallsNeedingCoverageToday = needsForTodayWindow.Sum(need => need.TotalRequiredBalls);
        var coverageWindowEndDate = needsForTodayWindow.Length == 0
            ? request.ProductionDate
            : needsForTodayWindow.Max(need => need.NeedDate);

        var fermentingBallsReadyInWindow = doughBatches
            .Where(batch =>
                batch.FermentationReadyDate > request.ProductionDate &&
                batch.FermentationReadyDate <= coverageWindowEndDate)
            .Sum(batch => batch.TotalBalls);

        var readyUnballedBalls = doughBatches
            .Where(batch =>
                !batch.IsBalled &&
                batch.FermentationReadyDate <= coverageWindowEndDate)
            .Sum(batch => batch.TotalBalls);

        var availableCoverageForWindow = readyBalls + fermentingBallsReadyInWindow + readyUnballedBalls;
        var missingBallsForProductionWindow = Math.Max(totalBallsNeedingCoverageToday - availableCoverageForWindow, 0);
        var recommendedCasesToMakeToday = CalculateRoundedUpUnits(
            missingBallsForProductionWindow,
            DoughRules.BallsPerCase);
        var recommendedLoadsToMakeToday = CalculateRoundedUpUnits(
            recommendedCasesToMakeToday,
            DoughRules.StandardBatchCases);
        var recommendedBallsToBallToday = Math.Min(
            readyUnballedBalls,
            Math.Max(totalBallsNeedingCoverageToday - readyBalls, 0));

        return new DoughProductionPlanningResponse
        {
            ProductionDate = request.ProductionDate,
            TotalFutureRequiredBalls = totalFutureRequiredBalls,
            ReadyBalls = readyBalls,
            FermentingBalls = fermentingBalls,
            UnballedBalls = unballedBalls,
            MissingBallsForProductionWindow = missingBallsForProductionWindow,
            RecommendedCasesToMakeToday = recommendedCasesToMakeToday,
            RecommendedLoadsToMakeToday = recommendedLoadsToMakeToday,
            RecommendedBallsToBallToday = recommendedBallsToBallToday,
            UpcomingNeeds = upcomingNeedsReadOnly,
            Reason = BuildReason(
                request,
                needsForTodayWindow,
                readyBalls,
                fermentingBallsReadyInWindow,
                readyUnballedBalls,
                missingBallsForProductionWindow,
                recommendedCasesToMakeToday,
                recommendedLoadsToMakeToday,
                recommendedBallsToBallToday)
        };
    }

    private async Task<DoughNeedByDateResponse> BuildNeedByDateAsync(
        DateOnly needDate,
        IReadOnlyCollection<RestaurantEvent> upcomingEvents,
        CancellationToken cancellationToken)
    {
        var activeDemandPlans = await _doughDemandPlanReadRepository.GetActiveByDayOfWeekAsync(
            needDate.DayOfWeek,
            cancellationToken);

        var restaurantBaselineBalls = activeDemandPlans.Count > 0
            ? activeDemandPlans.Sum(plan => plan.GetBaselineDoughBalls())
            : await CalculateHistoricalAverageBallsAsync(needDate, cancellationToken);

        var eventsForNeedDate = upcomingEvents
            .Where(restaurantEvent => restaurantEvent.EventDate == needDate)
            .ToArray();

        var eventBalls = eventsForNeedDate.Sum(restaurantEvent => restaurantEvent.EstimatedDoughBalls);
        var usesShortFermentation = eventsForNeedDate.Any(restaurantEvent => restaurantEvent.AllowShortFermentation) &&
            DoughRules.IsSummerEventMonth(needDate.Month);

        var (productionWindowStart, productionWindowEnd, recommendedMakeDate) =
            DoughRules.GetProductionWindow(needDate, usesShortFermentation);

        return new DoughNeedByDateResponse
        {
            NeedDate = needDate,
            RestaurantBaselineBalls = restaurantBaselineBalls,
            EventBalls = eventBalls,
            TotalRequiredBalls = checked(restaurantBaselineBalls + eventBalls),
            ProductionWindowStart = productionWindowStart,
            ProductionWindowEnd = productionWindowEnd,
            RecommendedMakeDate = recommendedMakeDate,
            UsesShortFermentation = usesShortFermentation
        };
    }

    private async Task<int> CalculateHistoricalAverageBallsAsync(
        DateOnly needDate,
        CancellationToken cancellationToken)
    {
        var historicalSales = await _salesHistoryReadRepository.GetRecentByDayOfWeekAsync(
            needDate,
            DoughRules.DefaultHistoricalWeeksToUse,
            cancellationToken);

        if (historicalSales.Count == 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(historicalSales.Average(sale => sale.DoughBallsUsed));
    }

    private static void ValidateRequest(DoughProductionPlanningRequest request)
    {
        if (request.ProductionDate == default)
        {
            throw new ArgumentException("Production date is required.", nameof(request));
        }

        if (request.DaysAhead < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.DaysAhead), "Days ahead must be at least 1.");
        }
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
        DoughProductionPlanningRequest request,
        IReadOnlyCollection<DoughNeedByDateResponse> needsForTodayWindow,
        int readyBalls,
        int fermentingBallsReadyInWindow,
        int readyUnballedBalls,
        int missingBallsForProductionWindow,
        int recommendedCasesToMakeToday,
        int recommendedLoadsToMakeToday,
        int recommendedBallsToBallToday)
    {
        var reasonBuilder = new StringBuilder();

        if (needsForTodayWindow.Count == 0)
        {
            reasonBuilder.Append(
                $"No upcoming need dates in the next {request.DaysAhead} day(s) are scheduled to be produced on {request.ProductionDate:MMM d, yyyy}.");
        }
        else
        {
            reasonBuilder.Append(
                $"Today's production window covers {FormatNeedDates(needsForTodayWindow)}.");
        }

        reasonBuilder.Append($" Ready dough contributes {readyBalls} balls.");
        reasonBuilder.Append($" Dough already fermenting for this window contributes {fermentingBallsReadyInWindow} balls.");
        reasonBuilder.Append($" Existing unballed dough contributes {readyUnballedBalls} balls that can be portioned today.");

        if (missingBallsForProductionWindow > 0)
        {
            reasonBuilder.Append(
                $" After existing coverage, the team still needs {missingBallsForProductionWindow} balls, so the system recommends making {recommendedCasesToMakeToday} case(s) across {recommendedLoadsToMakeToday} load(s).");
        }
        else
        {
            reasonBuilder.Append(" Existing inventory and in-flight dough already cover the current production window, so no new mixing is recommended today.");
        }

        if (recommendedBallsToBallToday > 0)
        {
            reasonBuilder.Append($" Ball {recommendedBallsToBallToday} already-produced dough ball(s) today.");
        }
        else
        {
            reasonBuilder.Append(" No additional balling is required today from existing batches.");
        }

        return reasonBuilder.ToString();
    }

    private static string FormatNeedDates(IReadOnlyCollection<DoughNeedByDateResponse> needsForTodayWindow)
    {
        return string.Join(
            ", ",
            needsForTodayWindow.Select(need =>
                $"{need.NeedDate:ddd MMM d} ({need.TotalRequiredBalls} balls, make by {need.RecommendedMakeDate:MMM d})"));
    }
}
