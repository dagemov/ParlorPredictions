using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;
using ParlorPrediction.Mvc.Helpers;
using ParlorPrediction.Mvc.Models.Prep;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)},{nameof(ApplicationRole.PizzaMaker)}")]
[Route("prep")]
public sealed class PrepController : Controller
{
    private const int DefaultHistoricalWeeksToUse = 8;
    private const int DefaultPlanningDaysAhead = 7;

    private readonly IDoughPrepCalculationService _doughPrepCalculationService;
    private readonly IDoughQualityReadService _doughQualityReadService;
    private readonly IDoughProductionPlanningService _doughProductionPlanningService;
    private readonly IDoughPrepRecommendationReadService _doughPrepRecommendationReadService;
    private readonly IDoughPrepRecommendationService _doughPrepRecommendationService;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;
    private readonly IPrepTaskReadService _prepTaskReadService;
    private readonly IPrepTaskService _prepTaskService;

    public PrepController(
        IDoughPrepCalculationService doughPrepCalculationService,
        IDoughQualityReadService doughQualityReadService,
        IDoughProductionPlanningService doughProductionPlanningService,
        IDoughPrepRecommendationReadService doughPrepRecommendationReadService,
        IDoughPrepRecommendationService doughPrepRecommendationService,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService,
        IPrepTaskReadService prepTaskReadService,
        IPrepTaskService prepTaskService)
    {
        _doughPrepCalculationService = doughPrepCalculationService;
        _doughQualityReadService = doughQualityReadService;
        _doughProductionPlanningService = doughProductionPlanningService;
        _doughPrepRecommendationReadService = doughPrepRecommendationReadService;
        _doughPrepRecommendationService = doughPrepRecommendationService;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
        _prepTaskReadService = prepTaskReadService;
        _prepTaskService = prepTaskService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("help")]
    public IActionResult Help()
    {
        return View();
    }

    [HttpGet("dough")]
    public async Task<IActionResult> Dough(
        DateOnly? targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        var pageModel = await BuildPageViewModelAsync(
            selectedDate,
            null,
            historicalWeeksToUse,
            cancellationToken);

        return View(pageModel);
    }

    [HttpGet("dough/kitchen-sheet")]
    public async Task<IActionResult> KitchenSheet(
        DateOnly? targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        var normalizedHistoricalWeeks = NormalizeHistoricalWeeks(historicalWeeksToUse);
        var pageModel = await BuildPageViewModelAsync(
            selectedDate,
            null,
            normalizedHistoricalWeeks,
            cancellationToken);

        var warnings = new List<string>();
        var printableRecommendation = await EnsurePrintableRecommendationAsync(
            pageModel.Recommendation,
            selectedDate,
            normalizedHistoricalWeeks,
            warnings,
            cancellationToken);

        var doughQualitySummary = new Models.DoughQuality.DoughQualitySummaryViewModel();
        IReadOnlyList<DoughKitchenAttentionItemViewModel> attentionItems = Array.Empty<DoughKitchenAttentionItemViewModel>();

        try
        {
            doughQualitySummary = await BuildDoughQualitySummaryAsync(cancellationToken);
            attentionItems = await BuildKitchenAttentionItemsAsync(selectedDate, cancellationToken);
        }
        catch (Exception exception) when (IsRecoverableDoughQualityException(exception))
        {
            warnings.Add("Dough Quality data could not be fully loaded for printing. The kitchen sheet still shows the Dough Prep guidance that is available.");
        }

        return View(new DoughKitchenSheetPageViewModel
        {
            TargetDate = selectedDate,
            HistoricalWeeksToUse = normalizedHistoricalWeeks,
            Recommendation = printableRecommendation,
            ProductionPlanning = pageModel.ProductionPlanning,
            WeeklyGoal = pageModel.WeeklyGoal,
            QualitySummary = doughQualitySummary,
            AttentionItems = attentionItems,
            OpenTasks = pageModel.Tasks
                .Where(task => !string.Equals(task.Status, nameof(PrepTaskStatus.Completed), StringComparison.OrdinalIgnoreCase))
                .ToArray(),
            Warnings = warnings
        });
    }

    [HttpGet("dough/week")]
    public async Task<IActionResult> DoughWeek(
        DateOnly? targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        var calendar = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            selectedDate,
            NormalizeHistoricalWeeks(historicalWeeksToUse),
            cancellationToken);

        return View(new WeeklyDoughCalendarViewModel
        {
            SelectedDate = selectedDate,
            HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse),
            WeekStartDate = calendar.WeekStartDate,
            WeekEndDate = calendar.WeekEndDate,
            WeekAvailableBalls = calendar.WeekAvailableBalls,
            WeekTotalNeededBalls = calendar.WeekTotalNeededBalls,
            WeekCompletedBalls = calendar.WeekCompletedBalls,
            WeekMissingBalls = calendar.WeekMissingBalls,
            UpcomingEventBalls = calendar.UpcomingEventBalls,
            Days = calendar.Days
                .Select(day => new WeeklyDoughCalendarDayViewModel
                {
                    Date = day.Date,
                    RestaurantDoughBalls = day.RestaurantDoughBalls,
                    EventDoughBalls = day.EventDoughBalls,
                    TotalNeededBalls = day.TotalNeededBalls,
                    AvailableBalls = day.AvailableBalls,
                    CompletedBalls = day.CompletedBalls,
                    StillMissingBalls = day.StillMissingBalls,
                    Status = day.Status,
                    IsToday = day.Date == DateOnly.FromDateTime(DateTime.Today)
                })
                .ToArray()
        });
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("dough/calculate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateDough(
        DateOnly targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : targetDate;

        try
        {
            var calculation = await _doughPrepCalculationService.CalculateAsync(
                new CalculateDoughPrepRequest
                {
                    TargetDate = selectedDate,
                    HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse)
                },
                cancellationToken);

            var pageModel = await BuildPageViewModelAsync(
                selectedDate,
                MapCalculation(calculation, historicalWeeksToUse),
                historicalWeeksToUse,
                cancellationToken);

            SetAlert(
                "success",
                "Recommendation calculated",
                "The dough recommendation is ready in memory. Save it if you want an auditable snapshot.");

            return RenderDoughResult(pageModel);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException)
        {
            return await RenderErrorPageAsync(selectedDate, historicalWeeksToUse, exception.Message, cancellationToken);
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("dough/recommendation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDoughRecommendation(
        DateOnly targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : targetDate;

        try
        {
            var response = await _doughPrepRecommendationService.GenerateAsync(
                new GenerateDoughPrepRecommendationRequest
                {
                    TargetDate = selectedDate,
                    HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse)
                },
                cancellationToken);

            var pageModel = await BuildPageViewModelAsync(
                selectedDate,
                MapRecommendation(response, historicalWeeksToUse),
                historicalWeeksToUse,
                cancellationToken);

            SetAlert(
                "success",
                "Recommendation saved",
                "The dough recommendation was saved and can now be used to create a prep task.");

            return RenderDoughResult(pageModel);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException)
        {
            return await RenderErrorPageAsync(selectedDate, historicalWeeksToUse, exception.Message, cancellationToken);
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("dough/tasks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDoughTask(
        Guid recommendationId,
        DateOnly targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : targetDate;

        try
        {
            var response = await _prepTaskService.CreateFromDoughRecommendationAsync(
                new CreatePrepTaskFromRecommendationRequest
                {
                    DoughPrepRecommendationId = recommendationId
                },
                cancellationToken);

            var pageModel = await BuildPageViewModelAsync(
                selectedDate,
                null,
                historicalWeeksToUse,
                cancellationToken);

            SetAlert(
                response.TaskCreated ? "success" : "info",
                response.TaskCreated ? "Task ready" : "No task created",
                response.Message);

            return RenderDoughResult(pageModel);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException or
            KeyNotFoundException)
        {
            return await RenderErrorPageAsync(selectedDate, historicalWeeksToUse, exception.Message, cancellationToken);
        }
    }

    [HttpPost("dough/tasks/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteTask(
        CompletePrepTaskFormModel model,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = model.TargetDate == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : model.TargetDate;

        if (!ModelState.IsValid)
        {
            return await RenderErrorPageAsync(
                selectedDate,
                NormalizeHistoricalWeeks(model.HistoricalWeeksToUse),
                "Choose dough balls, cases, or full loads and enter the quantity completed.",
                cancellationToken);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var task = await _prepTaskReadService.GetByIdAsync(model.PrepTaskId, cancellationToken);
        if (task is null)
        {
            return await RenderErrorPageAsync(
                selectedDate,
                NormalizeHistoricalWeeks(model.HistoricalWeeksToUse),
                "The prep task could not be found.",
                cancellationToken);
        }

        if (User.IsInRole(nameof(ApplicationRole.PizzaMaker)) &&
            !User.IsInRole(nameof(ApplicationRole.Manager)) &&
            !User.IsInRole(nameof(ApplicationRole.Admin)) &&
            !string.Equals(task.AssignedRole, nameof(ApplicationRole.PizzaMaker), StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        try
        {
            if (!TryResolveCompletedBalls(model, out var completedBalls, out var validationMessage))
            {
                return await RenderErrorPageAsync(
                    selectedDate,
                    NormalizeHistoricalWeeks(model.HistoricalWeeksToUse),
                    validationMessage,
                    cancellationToken);
            }

            var response = await _prepTaskService.CompleteAsync(
                new CompletePrepTaskRequest
                {
                    PrepTaskId = model.PrepTaskId,
                    CompletedByUserId = currentUserId,
                    QuantityCompleted = completedBalls,
                    Notes = model.Notes
                },
                cancellationToken);

            var pageModel = await BuildPageViewModelAsync(
                selectedDate,
                null,
                NormalizeHistoricalWeeks(model.HistoricalWeeksToUse),
                cancellationToken);

            SetAlert("success", "Task completed", response.Message);
            return RenderDoughResult(pageModel);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException or
            KeyNotFoundException)
        {
            return await RenderErrorPageAsync(
                selectedDate,
                NormalizeHistoricalWeeks(model.HistoricalWeeksToUse),
                exception.Message,
                cancellationToken);
        }
    }

    private async Task<DoughPrepPageViewModel> BuildPageViewModelAsync(
        DateOnly targetDate,
        DoughRecommendationViewModel? recommendationOverride,
        int historicalWeeksToUse,
        CancellationToken cancellationToken)
    {
        var productionPlanning = await _doughProductionPlanningService.PlanAsync(
            new DoughProductionPlanningRequest
            {
                ProductionDate = targetDate,
                DaysAhead = DefaultPlanningDaysAhead
            },
            cancellationToken);

        var weeklyCalendar = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            targetDate,
            NormalizeHistoricalWeeks(historicalWeeksToUse),
            cancellationToken);

        var taskResponses = await _prepTaskReadService.GetDoughTasksByDateAsync(targetDate, cancellationToken);
        var taskViewModels = taskResponses
            .Select(MapTask)
            .Select(task =>
            {
                task.HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse);
                return task;
            })
            .ToArray();

        var recommendation = recommendationOverride;
        if (recommendation is null)
        {
            var savedRecommendation = await _doughPrepRecommendationReadService.GetLatestByDateAsync(targetDate, cancellationToken);
            recommendation = savedRecommendation is null
                ? null
                : MapRecommendation(savedRecommendation, historicalWeeksToUse);
        }

        if (recommendation is not null)
        {
            recommendation.HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse);
            recommendation.CompletedBalls = taskResponses
                .Where(task => string.Equals(task.Status, nameof(PrepTaskStatus.Completed), StringComparison.OrdinalIgnoreCase))
                .Sum(task => task.QuantityCompleted);
            recommendation.MissingBalls = Math.Max(
                recommendation.RequiredBalls - recommendation.AvailableBalls - recommendation.CompletedBalls,
                0);
            recommendation.CanSaveRecommendation = CanManageRecommendations() && !recommendation.IsPersisted;

            var taskAlreadyExists = recommendation.RecommendationId.HasValue &&
                taskResponses.Any(task => task.DoughPrepRecommendationId == recommendation.RecommendationId);

            recommendation.TaskAlreadyExists = taskAlreadyExists;
            recommendation.CanCreateTask = CanManageRecommendations() &&
                recommendation.IsPersisted &&
                recommendation.MissingBalls > 0 &&
                !taskAlreadyExists;
        }

        return new DoughPrepPageViewModel
        {
            TargetDate = targetDate,
            HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse),
            Recommendation = recommendation,
            ProductionPlanning = MapProductionPlanning(productionPlanning),
            WeeklyGoal = MapWeeklyGoal(weeklyCalendar),
            Tasks = taskViewModels,
            CanManageRecommendations = CanManageRecommendations()
        };
    }

    private async Task<DoughRecommendationViewModel?> EnsurePrintableRecommendationAsync(
        DoughRecommendationViewModel? recommendation,
        DateOnly targetDate,
        int historicalWeeksToUse,
        IList<string> warnings,
        CancellationToken cancellationToken)
    {
        if (recommendation is not null)
        {
            return recommendation;
        }

        try
        {
            var calculation = await _doughPrepCalculationService.CalculateAsync(
                new CalculateDoughPrepRequest
                {
                    TargetDate = targetDate,
                    HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse)
                },
                cancellationToken);

            warnings.Add("No saved dough snapshot existed for this day, so the kitchen sheet uses a fresh live calculation.");
            return MapCalculation(calculation, historicalWeeksToUse);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException)
        {
            warnings.Add("No dough recommendation could be calculated for this day. Print the sheet only after the team reviews the missing guidance.");
            return null;
        }
    }

    private async Task<Models.DoughQuality.DoughQualitySummaryViewModel> BuildDoughQualitySummaryAsync(CancellationToken cancellationToken)
    {
        var summary = await _doughQualityReadService.GetSummaryAsync(cancellationToken);

        return new Models.DoughQuality.DoughQualitySummaryViewModel
        {
            GoodBalls = summary.GoodBalls,
            AttentionBalls = summary.AttentionBalls,
            ReballedBalls = summary.ReballedBalls,
            MustUseNextDayBalls = summary.MustUseNextDayBalls,
            DiscardedBalls = summary.DiscardedBalls,
            TotalAvailableBalls = summary.TotalAvailableBalls
        };
    }

    private async Task<IReadOnlyList<DoughKitchenAttentionItemViewModel>> BuildKitchenAttentionItemsAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        var mustUseRecords = await _doughQualityReadService.SearchAsync(
            new SearchDoughBatchQualityRecordsRequest
            {
                CurrentStatus = nameof(DoughQualityStatus.MustUseNextDay)
            },
            cancellationToken);

        var attentionRecords = await _doughQualityReadService.SearchAsync(
            new SearchDoughBatchQualityRecordsRequest
            {
                CurrentStatus = nameof(DoughQualityStatus.Attention)
            },
            cancellationToken);

        var candidates = await _doughQualityReadService.EvaluateAttentionCandidatesAsync(
            new EvaluateDoughAttentionCandidatesRequest
            {
                ReferenceDate = targetDate
            },
            cancellationToken);

        var items = new List<DoughKitchenAttentionItemViewModel>();
        var seenIds = new HashSet<Guid>();

        foreach (var record in mustUseRecords.OrderBy(item => item.MustUseByDate).ThenBy(item => item.CreatedOrBalledAt))
        {
            if (!seenIds.Add(record.Id))
            {
                continue;
            }

            items.Add(new DoughKitchenAttentionItemViewModel
            {
                Title = $"Use first: dough from {record.SourceDate:ddd, MMM d}",
                StatusText = "Must Use Next Day",
                QuantityBalls = record.QuantityBalls,
                Detail = "Use this first.",
                SecondaryDetail = record.MustUseByDate.HasValue
                    ? $"Must use by {record.MustUseByDate.Value:MMM d, yyyy}"
                    : null,
                IsMustUsePriority = true
            });
        }

        foreach (var record in attentionRecords.OrderBy(item => item.SourceDate).ThenBy(item => item.CreatedOrBalledAt))
        {
            if (!seenIds.Add(record.Id))
            {
                continue;
            }

            items.Add(new DoughKitchenAttentionItemViewModel
            {
                Title = $"Review dough from {record.SourceDate:ddd, MMM d}",
                StatusText = Models.DoughQuality.DoughQualityDisplayText.Format(record.CurrentStatus),
                QuantityBalls = record.QuantityBalls,
                Detail = string.IsNullOrWhiteSpace(record.StatusReason)
                    ? "Still counts, but review it."
                    : record.StatusReason,
                SecondaryDetail = $"Created {record.CreatedOrBalledAt.ToLocalTime():MMM d, h:mm tt}",
                IsMustUsePriority = false
            });
        }

        foreach (var candidate in candidates.OrderBy(item => item.CreatedOrBalledAt))
        {
            if (!seenIds.Add(candidate.DoughBatchQualityRecordId))
            {
                continue;
            }

            items.Add(new DoughKitchenAttentionItemViewModel
            {
                Title = $"Check dough from {candidate.SourceDate:ddd, MMM d}",
                StatusText = Models.DoughQuality.DoughQualityDisplayText.Format(candidate.CurrentStatus),
                QuantityBalls = candidate.QuantityBalls,
                Detail = candidate.CandidateReason,
                SecondaryDetail = $"{candidate.AgeDays} operational day{(candidate.AgeDays == 1 ? string.Empty : "s")} old",
                IsMustUsePriority = false
            });
        }

        return items;
    }

    private IActionResult RenderDoughResult(DoughPrepPageViewModel pageModel)
    {
        if (Request.Headers.TryGetValue("HX-Request", out var headerValue) &&
            string.Equals(headerValue.ToString(), "true", StringComparison.OrdinalIgnoreCase))
        {
            return PartialView("_DoughPrepWorkspacePartial", pageModel);
        }

        return View("Dough", pageModel);
    }

    private async Task<IActionResult> RenderErrorPageAsync(
        DateOnly targetDate,
        int historicalWeeksToUse,
        string message,
        CancellationToken cancellationToken)
    {
        var pageModel = await BuildPageViewModelAsync(
            targetDate,
            null,
            historicalWeeksToUse,
            cancellationToken);

        SetAlert("error", "Action failed", message);
        return RenderDoughResult(pageModel);
    }

    private DoughTaskViewModel MapTask(DoughTaskListItemResponse task)
    {
        return new DoughTaskViewModel
        {
            PrepTaskId = task.PrepTaskId,
            DoughPrepRecommendationId = task.DoughPrepRecommendationId,
            TaskDate = task.TaskDate,
            PrepItemId = task.PrepItemId,
            PrepItemName = task.PrepItemName,
            PrepItemCode = task.PrepItemCode,
            PrepStationId = task.PrepStationId,
            PrepStationName = task.PrepStationName,
            PrepStationCode = task.PrepStationCode,
            AssignedRole = task.AssignedRole,
            QuantityRecommended = task.QuantityRecommended,
            QuantityCompleted = task.QuantityCompleted,
            Status = task.Status,
            Notes = task.Notes,
            CompletedByUserId = task.CompletedByUserId,
            CompletedByUserName = task.CompletedByUserName,
            CompletedAtUtc = task.CompletedAtUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            IsManualTask = task.IsManualTask,
            CanComplete = CanCompleteTask(task)
        };
    }

    private static DoughRecommendationViewModel MapCalculation(
        DoughPrepCalculationResult calculation,
        int historicalWeeksToUse)
    {
        var actionPlanSteps = BuildRecommendationActionPlan(
            calculation.MissingBalls,
            calculation.RecommendedCases,
            calculation.RecommendedLoads,
            calculation.ShouldBallDough,
            calculation.UsesShortFermentationException,
            calculation.EventEstimatedBalls);

        return new DoughRecommendationViewModel
        {
            RecommendationDate = calculation.TargetDate,
            RequiredBalls = calculation.RequiredBalls,
            HistoricalAverageBalls = calculation.HistoricalAverageBalls,
            EventEstimatedBalls = calculation.EventEstimatedBalls,
            AvailableBalls = calculation.AvailableBalls,
            CompletedBalls = calculation.CompletedBalls,
            MissingBalls = calculation.MissingBalls,
            RecommendedCases = calculation.RecommendedCases,
            RecommendedLoads = calculation.RecommendedLoads,
            ShouldMakeDough = calculation.ShouldMakeDough,
            ShouldBallDough = calculation.ShouldBallDough,
            UsesShortFermentationException = calculation.UsesShortFermentationException,
            Reason = calculation.Reason,
            HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse),
            IsPersisted = false,
            ActionPlanSteps = actionPlanSteps
        };
    }

    private static DoughRecommendationViewModel MapRecommendation(
        DoughRecommendationDetailResponse recommendation,
        int historicalWeeksToUse)
    {
        var actionPlanSteps = BuildRecommendationActionPlan(
            recommendation.MissingBalls,
            recommendation.RecommendedCases,
            recommendation.RecommendedLoads,
            recommendation.ShouldBallDough,
            recommendation.UsesShortFermentationException,
            recommendation.EventEstimatedBalls);

        return new DoughRecommendationViewModel
        {
            RecommendationId = recommendation.RecommendationId,
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
            HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse),
            IsPersisted = true,
            SavedAtUtc = recommendation.CreatedAtUtc,
            ActionPlanSteps = actionPlanSteps
        };
    }

    private static DoughRecommendationViewModel MapRecommendation(
        GenerateDoughPrepRecommendationResponse recommendation,
        int historicalWeeksToUse)
    {
        var actionPlanSteps = BuildRecommendationActionPlan(
            recommendation.MissingBalls,
            recommendation.RecommendedCases,
            recommendation.RecommendedLoads,
            recommendation.ShouldBallDough,
            recommendation.UsesShortFermentationException,
            recommendation.EventEstimatedBalls);

        return new DoughRecommendationViewModel
        {
            RecommendationId = recommendation.RecommendationId,
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
            HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse),
            IsPersisted = true,
            SavedAtUtc = DateTime.UtcNow,
            ActionPlanSteps = actionPlanSteps
        };
    }

    private static DoughProductionPlanningViewModel MapProductionPlanning(
        DoughProductionPlanningResponse productionPlanning)
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

    private static WeeklyGoalProgressViewModel MapWeeklyGoal(WeeklyDoughCalendarResponse weeklyCalendar)
    {
        return new WeeklyGoalProgressViewModel
        {
            WeekStartDate = weeklyCalendar.WeekStartDate,
            WeekEndDate = weeklyCalendar.WeekEndDate,
            CurrentAvailableBalls = weeklyCalendar.WeekAvailableBalls,
            DoughNeededBalls = weeklyCalendar.WeekTotalNeededBalls,
            DoughFinishedBalls = weeklyCalendar.WeekCompletedBalls,
            DoughStillMissingBalls = weeklyCalendar.WeekMissingBalls
        };
    }

    private bool CanManageRecommendations()
    {
        return User.IsInRole(nameof(ApplicationRole.Admin)) || User.IsInRole(nameof(ApplicationRole.Manager));
    }

    private bool CanCompleteTask(DoughTaskListItemResponse task)
    {
        if (string.Equals(task.Status, nameof(PrepTaskStatus.Completed), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(task.Status, nameof(PrepTaskStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (User.IsInRole(nameof(ApplicationRole.Admin)) || User.IsInRole(nameof(ApplicationRole.Manager)))
        {
            return true;
        }

        return User.IsInRole(nameof(ApplicationRole.PizzaMaker)) &&
            string.Equals(task.AssignedRole, nameof(ApplicationRole.PizzaMaker), StringComparison.OrdinalIgnoreCase);
    }

    private void SetAlert(string type, string title, string message)
    {
        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new
        {
            prepAlert = new
            {
                type,
                title,
                message
            }
        });
    }

    private static int NormalizeHistoricalWeeks(int historicalWeeksToUse)
    {
        return historicalWeeksToUse < 1 ? DefaultHistoricalWeeksToUse : historicalWeeksToUse;
    }

    private static bool IsRecoverableDoughQualityException(Exception exception)
    {
        if (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or KeyNotFoundException)
        {
            return true;
        }

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 208 } sqlException &&
                (sqlException.Message.Contains("DoughBatchQualityRecords", StringComparison.OrdinalIgnoreCase) ||
                 sqlException.Message.Contains("DoughLossRecords", StringComparison.OrdinalIgnoreCase) ||
                 sqlException.Message.Contains("DoughReballRecords", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveCompletedBalls(
        CompletePrepTaskFormModel model,
        out int completedBalls,
        out string validationMessage)
    {
        return DoughQuantityInputConverter.TryConvertToBalls(
            model.CompletionType,
            model.QuantityValue,
            out completedBalls,
            out validationMessage);
    }

    private static IReadOnlyList<string> BuildRecommendationActionPlan(
        int missingBalls,
        int recommendedCases,
        int recommendedLoads,
        bool shouldBallDough,
        bool usesShortFermentationException,
        int eventEstimatedBalls)
    {
        var steps = new List<string>();

        if (missingBalls <= 0)
        {
            steps.Add("No new dough batch is needed for this day. Use the dough already available and keep an eye on the next prep date.");
        }
        else if (recommendedLoads > 0)
        {
            var producedBalls = recommendedLoads * DoughRules.StandardBatchBalls;
            steps.Add(
                $"Make {recommendedLoads} full dough batch{(recommendedLoads == 1 ? string.Empty : "es")} today. That gives the kitchen about {producedBalls} dough balls and covers the current shortage of {missingBalls}.");

            if (recommendedLoads == 1 && missingBalls < DoughRules.StandardBatchBalls)
            {
                steps.Add("One full batch will leave extra dough that can roll into the next prep day if fermentation timing still works.");
            }
            else
            {
                steps.Add($"Plan for about {recommendedCases} case{(recommendedCases == 1 ? string.Empty : "s")} of dough from that production run.");
            }
        }

        if (shouldBallDough)
        {
            steps.Add("Ball any dough that has already finished fermenting so the team can use it without delay.");
        }

        if (eventEstimatedBalls > 0)
        {
            steps.Add(
                usesShortFermentationException
                    ? "Part of this dough supports an upcoming summer event, so the shorter 1 to 2 day fermentation window may be used if the manager confirms it."
                    : "Part of this dough supports an upcoming event, so make sure that dough is mixed ahead of the event date.");
        }

        return steps;
    }
}
