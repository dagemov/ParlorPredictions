using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Mvc.Models;
using ParlorPrediction.Mvc.Models.DoughInventory;
using ParlorPrediction.Mvc.Models.DoughQuality;
using ParlorPrediction.Mvc.Models.Home;
using ParlorPrediction.Mvc.Models.Prep;

namespace ParlorPrediction.Mvc.Controllers;

public class HomeController : Controller
{
    private const int DefaultHistoricalWeeksToUse = 8;
    private const int DefaultPlanningDaysAhead = 7;

    private readonly IDailyDoughClosingReadService _dailyDoughClosingReadService;
    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;
    private readonly IDoughProductionPlanningService _doughProductionPlanningService;
    private readonly IDoughQualityReadService _doughQualityReadService;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;
    private readonly IRestaurantEventReadRepository _restaurantEventReadRepository;

    public HomeController(
        IDailyDoughClosingReadService dailyDoughClosingReadService,
        IDoughInventoryImpactReadService doughInventoryImpactReadService,
        IDoughProductionPlanningService doughProductionPlanningService,
        IDoughQualityReadService doughQualityReadService,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService,
        IRestaurantEventReadRepository restaurantEventReadRepository)
    {
        _dailyDoughClosingReadService = dailyDoughClosingReadService;
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
        _doughProductionPlanningService = doughProductionPlanningService;
        _doughQualityReadService = doughQualityReadService;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
        _restaurantEventReadRepository = restaurantEventReadRepository;
    }

    public async Task<IActionResult> Index(
        DateOnly? targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var canUseOperationalHome = User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Admin") || User.IsInRole("Manager") || User.IsInRole("PizzaMaker"));

        if (!canUseOperationalHome)
        {
            return View(new OperationalHomePageViewModel
            {
                IsAuthenticatedExperience = false
            });
        }

        ViewData["UseDoughExperience"] = true;

        var selectedDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        var normalizedHistoricalWeeks = historicalWeeksToUse < 1
            ? DefaultHistoricalWeeksToUse
            : historicalWeeksToUse;

        var qualitySummary = new DoughQualitySummaryViewModel();
        try
        {
            var quality = await _doughQualityReadService.GetSummaryAsync(cancellationToken);
            qualitySummary = new DoughQualitySummaryViewModel
            {
                GoodBalls = quality.GoodBalls,
                AttentionBalls = quality.AttentionBalls,
                ReballedBalls = quality.ReballedBalls,
                MustUseNextDayBalls = quality.MustUseNextDayBalls,
                DiscardedBalls = quality.DiscardedBalls,
                TotalAvailableBalls = quality.TotalAvailableBalls
            };
        }
        catch (Exception exception) when (IsRecoverableDoughQualityException(exception))
        {
            qualitySummary = new DoughQualitySummaryViewModel();
        }

        var productionPlanning = await _doughProductionPlanningService.PlanAsync(
            new DoughProductionPlanningRequest
            {
                ProductionDate = selectedDate,
                DaysAhead = DefaultPlanningDaysAhead
            },
            cancellationToken);

        var weeklyCalendar = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            selectedDate,
            normalizedHistoricalWeeks,
            cancellationToken);

        var events = await _restaurantEventReadRepository.GetBetweenDatesAsync(
            weeklyCalendar.WeekStartDate,
            weeklyCalendar.WeekEndDate,
            cancellationToken);

        var dailyClosingInsights = new DailyClosingOperationalInsightsViewModel();
        try
        {
            dailyClosingInsights = MapDailyClosingInsights(await _dailyDoughClosingReadService.GetOperationalInsightsAsync(
                new GetDailyClosingWeekSummaryRequest
                {
                    ReferenceDate = selectedDate,
                    HistoricalWeeksToUse = normalizedHistoricalWeeks
                },
                cancellationToken));
        }
        catch (Exception exception) when (IsRecoverableDoughQualityException(exception))
        {
            dailyClosingInsights = new DailyClosingOperationalInsightsViewModel();
        }

        var doughInventoryPreview = new DoughInventorySummaryViewModel
        {
            ReferenceDate = selectedDate
        };
        try
        {
            var inventoryImpact = await _doughInventoryImpactReadService.GetInventoryImpactAsync(
                new GetDoughInventoryImpactRequest
                {
                    ReferenceDate = selectedDate,
                    HistoricalWeeksToUse = normalizedHistoricalWeeks
                },
                cancellationToken);
            doughInventoryPreview = DoughInventoryViewModelMapper.MapSummary(inventoryImpact);
        }
        catch (Exception exception) when (IsRecoverableDoughQualityException(exception))
        {
            doughInventoryPreview = new DoughInventorySummaryViewModel
            {
                ReferenceDate = selectedDate
            };
        }

        return View(new OperationalHomePageViewModel
        {
            IsAuthenticatedExperience = true,
            CanManageEvents = User.IsInRole("Admin") || User.IsInRole("Manager"),
            CanSeeAdminPanel = User.IsInRole("Admin") || User.IsInRole("Manager"),
            TargetDate = selectedDate,
            HistoricalWeeksToUse = normalizedHistoricalWeeks,
            ProductionPlanning = MapProductionPlanning(productionPlanning),
            WeeklyGoal = MapWeeklyGoal(weeklyCalendar),
            QualitySummary = qualitySummary,
            WeeklyForecast = weeklyCalendar.Days
                .Select(day => new OperationalHomeWeekDayViewModel
                {
                    Date = day.Date,
                    TotalNeededBalls = day.TotalNeededBalls,
                    EventBalls = day.EventDoughBalls,
                    AvailableBalls = day.AvailableBalls,
                    CompletedBalls = day.CompletedBalls,
                    StillMissingBalls = day.StillMissingBalls,
                    Status = day.Status,
                    IsToday = day.Date == selectedDate
                })
                .ToArray(),
            Events = events
                .Where(item => item.IsActive)
                .OrderBy(item => item.EventDate)
                .Select(item => new OperationalHomeEventViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    EventDate = item.EventDate,
                    EstimatedDoughBalls = item.EstimatedDoughBalls,
                    AllowShortFermentation = item.AllowShortFermentation,
                    Notes = item.Notes
                })
                .ToArray(),
            DailyClosingInsights = dailyClosingInsights,
            DoughInventoryPreview = doughInventoryPreview
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static DoughProductionPlanningViewModel MapProductionPlanning(
        Contracts.Responses.Dough.DoughProductionPlanningResponse productionPlanning)
    {
        return new DoughProductionPlanningViewModel
        {
            ProductionDate = productionPlanning.ProductionDate,
            DaysAhead = productionPlanning.UpcomingNeeds.Count,
            TotalFutureRequiredBalls = productionPlanning.TotalFutureRequiredBalls,
            ReadyBalls = productionPlanning.ReadyBalls,
            FermentingBalls = productionPlanning.FermentingBalls,
            UnballedBalls = productionPlanning.UnballedBalls,
            MissingBallsForProductionWindow = productionPlanning.MissingBallsForProductionWindow,
            RecommendedCasesToMakeToday = productionPlanning.RecommendedCasesToMakeToday,
            RecommendedLoadsToMakeToday = productionPlanning.RecommendedLoadsToMakeToday,
            RecommendedBallsToBallToday = productionPlanning.RecommendedBallsToBallToday,
            Reason = productionPlanning.Reason,
            UpcomingNeeds = productionPlanning.UpcomingNeeds
                .Select(need => new DoughNeedByDateViewModel
                {
                    NeedDate = need.NeedDate,
                    RestaurantBaselineBalls = need.RestaurantBaselineBalls,
                    EventBalls = need.EventBalls,
                    TotalRequiredBalls = need.TotalRequiredBalls,
                    ProductionWindowStart = need.ProductionWindowStart,
                    ProductionWindowEnd = need.ProductionWindowEnd,
                    RecommendedMakeDate = need.RecommendedMakeDate,
                    UsesShortFermentation = need.UsesShortFermentation,
                    IsRecommendedForSelectedProductionDate = need.RecommendedMakeDate == productionPlanning.ProductionDate
                })
                .ToArray()
        };
    }

    private static WeeklyGoalProgressViewModel MapWeeklyGoal(
        Contracts.Responses.Prep.WeeklyDoughCalendarResponse weeklyCalendar)
    {
        return new WeeklyGoalProgressViewModel
        {
            WeekStartDate = weeklyCalendar.WeekStartDate,
            WeekEndDate = weeklyCalendar.WeekEndDate,
            HasClosingCarryover = weeklyCalendar.HasClosingCarryover,
            CarryoverSourceWeekStartDate = weeklyCalendar.CarryoverSourceWeekStartDate,
            CarryoverSourceWeekEndDate = weeklyCalendar.CarryoverSourceWeekEndDate,
            CarryoverReadyBalls = weeklyCalendar.CarryoverReadyBalls,
            CarryoverAttentionBalls = weeklyCalendar.CarryoverAttentionBalls,
            CarryoverAvailableBalls = weeklyCalendar.CarryoverAvailableBalls,
            CarryoverMixedButNotBalledLoads = weeklyCalendar.CarryoverMixedButNotBalledLoads,
            CarryoverMixedButNotBalledPotentialBalls = weeklyCalendar.CarryoverMixedButNotBalledPotentialBalls,
            PreviousWeekProducedBalls = weeklyCalendar.PreviousWeekProducedBalls,
            PreviousWeekLostBalls = weeklyCalendar.PreviousWeekLostBalls,
            CarryoverClosingNotes = weeklyCalendar.CarryoverClosingNotes,
            DoughNeededBalls = weeklyCalendar.WeekTotalNeededBalls,
            ReadyNowBalls = weeklyCalendar.ReadyNowBalls,
            StillFermentingBalls = weeklyCalendar.StillFermentingBalls,
            MixedButNotBalledBalls = weeklyCalendar.MixedButNotBalledBalls,
            MixedButNotBalledLoadCount = weeklyCalendar.MixedButNotBalledLoads,
            FutureBalls = weeklyCalendar.FutureBalls,
            FinishedThisWeekBalls = weeklyCalendar.FinishedThisWeekBalls,
            ProducedThisWeekBalls = weeklyCalendar.ProducedThisWeekBalls,
            PreviousWeekFinishedBalls = weeklyCalendar.PreviousWeekFinishedBalls,
            DoughStillMissingThisWeekBalls = weeklyCalendar.StillMissingThisWeekBalls,
            ActualUsedBallsThisWeek = weeklyCalendar.ActualUsedBallsThisWeek,
            AccumulatedDailyVariance = weeklyCalendar.AccumulatedDailyVariance
        };
    }

    private static DailyClosingOperationalInsightsViewModel MapDailyClosingInsights(
        Contracts.Responses.DoughClosing.DailyClosingOperationalInsightsResponse insights)
    {
        return new DailyClosingOperationalInsightsViewModel
        {
            AccumulatedVariance = insights.AccumulatedVariance,
            AccumulatedSurplus = insights.AccumulatedSurplus,
            AccumulatedShortage = insights.AccumulatedShortage,
            TotalActualUsedBalls = insights.TotalActualUsedBalls,
            ClosedDaysCount = insights.ClosedDaysCount,
            CurrentAvailableBalls = insights.CurrentAvailableBalls,
            StillFermentingBalls = insights.StillFermentingBalls,
            MixedButNotBalledBalls = insights.MixedButNotBalledBalls,
            RemainingForecastNeed = insights.RemainingForecastNeed,
            AdjustedRemainingForecastNeed = insights.AdjustedRemainingForecastNeed,
            DailyClosingVarianceApplied = insights.DailyClosingVarianceApplied,
            ProjectedSurplus = insights.ProjectedSurplus,
            HasSurplusWarning = insights.HasSurplusWarning,
            HasShortageWarning = insights.HasShortageWarning,
            TotalTracedUsedBallsOnClosedDays = insights.TotalTracedUsedBallsOnClosedDays,
            TraceReconciliationDifferenceBalls = insights.TraceReconciliationDifferenceBalls,
            HasTraceReconciliationWarning = insights.HasTraceReconciliationWarning,
            TraceReconciliationMessage = insights.TraceReconciliationMessage,
            Recommendation = insights.Recommendation
        };
    }

    private static bool IsRecoverableDoughQualityException(Exception exception)
    {
        if (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or KeyNotFoundException)
        {
            return true;
        }

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 208 })
            {
                return true;
            }
        }

        return false;
    }
}
