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
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public DoughProductionPlanningService(
        IDoughBatchReadRepository doughBatchReadRepository,
        IDoughDemandPlanReadRepository doughDemandPlanReadRepository,
        IDoughInventoryReadRepository doughInventoryReadRepository,
        IRestaurantEventReadRepository restaurantEventReadRepository,
        ISalesHistoryReadRepository salesHistoryReadRepository,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _doughBatchReadRepository = doughBatchReadRepository;
        _doughDemandPlanReadRepository = doughDemandPlanReadRepository;
        _doughInventoryReadRepository = doughInventoryReadRepository;
        _restaurantEventReadRepository = restaurantEventReadRepository;
        _salesHistoryReadRepository = salesHistoryReadRepository;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
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
        var carryover = await GetCarryoverFallbackAsync(request.ProductionDate, latestInventorySnapshot, cancellationToken);
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
        var readyBalls = carryover?.CarryoverAvailableBalls
            ?? latestInventorySnapshot?.AvailableBalls
            ?? 0;

        var liveFermentingBalls = doughBatches
            .Where(batch => batch.FermentationReadyDate > request.ProductionDate)
            .Sum(batch => batch.TotalBalls);

        var liveUnballedBalls = doughBatches
            .Where(batch => !batch.IsBalled)
            .Sum(batch => batch.TotalBalls);
        var carryoverUnballedBalls = liveUnballedBalls == 0
            ? carryover?.MixedButNotBalledLoads * DoughRules.StandardBatchBalls ?? 0
            : 0;
        var fermentingBalls = liveFermentingBalls;
        var unballedBalls = liveUnballedBalls + carryoverUnballedBalls;

        var totalBallsNeedingCoverageToday = needsForTodayWindow.Sum(need => need.TotalRequiredBalls);
        var coverageWindowEndDate = needsForTodayWindow.Length == 0
            ? request.ProductionDate
            : needsForTodayWindow.Max(need => need.NeedDate);

        var fermentingBallsReadyInWindow = doughBatches
            .Where(batch =>
                batch.FermentationReadyDate > request.ProductionDate &&
                batch.FermentationReadyDate <= coverageWindowEndDate)
            .Sum(batch => batch.TotalBalls);

        var liveReadyUnballedBalls = doughBatches
            .Where(batch =>
                !batch.IsBalled &&
                batch.FermentationReadyDate <= coverageWindowEndDate)
            .Sum(batch => batch.TotalBalls);
        var readyUnballedBalls = liveReadyUnballedBalls + carryoverUnballedBalls;

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
                $"There are no upcoming service days in the next {request.DaysAhead} days that need fresh dough mixed on {request.ProductionDate:MMM d, yyyy}.");
        }
        else
        {
            reasonBuilder.Append(
                $"Today's mixing work helps cover {FormatNeedDates(needsForTodayWindow)}.");
        }

        reasonBuilder.Append($" You already have {readyBalls} dough balls ready to use.");
        reasonBuilder.Append($" Another {fermentingBallsReadyInWindow} dough balls are still fermenting and should be ready in time for this window.");
        reasonBuilder.Append($" There are also {readyUnballedBalls} dough balls already mixed that can be balled today.");

        if (missingBallsForProductionWindow > 0)
        {
            reasonBuilder.Append(
                $" After that coverage, the kitchen is still short by {missingBallsForProductionWindow} dough balls, so today's plan is to make about {recommendedCasesToMakeToday} case{(recommendedCasesToMakeToday == 1 ? string.Empty : "s")} across {recommendedLoadsToMakeToday} full batch{(recommendedLoadsToMakeToday == 1 ? string.Empty : "es")}.");
        }
        else
        {
            reasonBuilder.Append(" Existing dough and dough already in progress cover this production window, so no new mixing is needed today.");
        }

        if (recommendedBallsToBallToday > 0)
        {
            reasonBuilder.Append($" Ball about {recommendedBallsToBallToday} dough balls from existing batches today so the line is ready when service starts.");
        }
        else
        {
            reasonBuilder.Append(" No extra balling work is needed from existing batches today.");
        }

        return reasonBuilder.ToString();
    }

    private static string FormatNeedDates(IReadOnlyCollection<DoughNeedByDateResponse> needsForTodayWindow)
    {
        return string.Join(
            ", ",
            needsForTodayWindow.Select(need =>
                $"{need.NeedDate:ddd MMM d} ({need.TotalRequiredBalls} balls, best made on {need.RecommendedMakeDate:MMM d})"));
    }

    private async Task<Contracts.Responses.DoughClosing.WeeklyDoughCarryoverResponse?> GetCarryoverFallbackAsync(
        DateOnly productionDate,
        DoughInventorySnapshot? latestInventorySnapshot,
        CancellationToken cancellationToken)
    {
        var weekStartDate = GetOperationalWeekStart(productionDate);
        var hasCurrentWeekSnapshot = latestInventorySnapshot is not null &&
            latestInventorySnapshot.SnapshotDate >= weekStartDate;

        if (hasCurrentWeekSnapshot)
        {
            return null;
        }

        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new Contracts.Requests.DoughClosing.GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = productionDate
            },
            cancellationToken);

        return carryover.HasClosingCarryover
            ? carryover
            : null;
    }

    private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }
}
