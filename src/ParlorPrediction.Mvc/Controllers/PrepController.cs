using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.Prep;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)},{nameof(ApplicationRole.PizzaMaker)}")]
[Route("prep")]
public sealed class PrepController : Controller
{
    private const int DefaultHistoricalWeeksToUse = 8;
    private const int DefaultPlanningDaysAhead = 7;

    private readonly IDoughPrepCalculationService _doughPrepCalculationService;
    private readonly IDoughProductionPlanningService _doughProductionPlanningService;
    private readonly IDoughPrepRecommendationReadService _doughPrepRecommendationReadService;
    private readonly IDoughPrepRecommendationService _doughPrepRecommendationService;
    private readonly IPrepTaskReadService _prepTaskReadService;
    private readonly IPrepTaskService _prepTaskService;

    public PrepController(
        IDoughPrepCalculationService doughPrepCalculationService,
        IDoughProductionPlanningService doughProductionPlanningService,
        IDoughPrepRecommendationReadService doughPrepRecommendationReadService,
        IDoughPrepRecommendationService doughPrepRecommendationService,
        IPrepTaskReadService prepTaskReadService,
        IPrepTaskService prepTaskService)
    {
        _doughPrepCalculationService = doughPrepCalculationService;
        _doughProductionPlanningService = doughProductionPlanningService;
        _doughPrepRecommendationReadService = doughPrepRecommendationReadService;
        _doughPrepRecommendationService = doughPrepRecommendationService;
        _prepTaskReadService = prepTaskReadService;
        _prepTaskService = prepTaskService;
    }

    [HttpGet("")]
    public IActionResult Index()
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

    [HttpPost("tasks/complete")]
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
                "Quantity completed must be greater than zero.",
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
            var response = await _prepTaskService.CompleteAsync(
                new CompletePrepTaskRequest
                {
                    PrepTaskId = model.PrepTaskId,
                    CompletedByUserId = currentUserId,
                    QuantityCompleted = model.QuantityCompleted,
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
            Tasks = taskViewModels,
            CanManageRecommendations = CanManageRecommendations()
        };
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
            PrepItemName = task.PrepItemName,
            PrepStationName = task.PrepStationName,
            AssignedRole = task.AssignedRole,
            QuantityRecommended = task.QuantityRecommended,
            QuantityCompleted = task.QuantityCompleted,
            Status = task.Status,
            CompletedAtUtc = task.CompletedAtUtc,
            CanComplete = CanCompleteTask(task)
        };
    }

    private static DoughRecommendationViewModel MapCalculation(
        DoughPrepCalculationResult calculation,
        int historicalWeeksToUse)
    {
        return new DoughRecommendationViewModel
        {
            RecommendationDate = calculation.TargetDate,
            RequiredBalls = calculation.RequiredBalls,
            HistoricalAverageBalls = calculation.HistoricalAverageBalls,
            EventEstimatedBalls = calculation.EventEstimatedBalls,
            AvailableBalls = calculation.AvailableBalls,
            MissingBalls = calculation.MissingBalls,
            RecommendedCases = calculation.RecommendedCases,
            RecommendedLoads = calculation.RecommendedLoads,
            ShouldMakeDough = calculation.ShouldMakeDough,
            ShouldBallDough = calculation.ShouldBallDough,
            UsesShortFermentationException = calculation.UsesShortFermentationException,
            Reason = calculation.Reason,
            HistoricalWeeksToUse = NormalizeHistoricalWeeks(historicalWeeksToUse),
            IsPersisted = false
        };
    }

    private static DoughRecommendationViewModel MapRecommendation(
        DoughRecommendationDetailResponse recommendation,
        int historicalWeeksToUse)
    {
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
            SavedAtUtc = recommendation.CreatedAtUtc
        };
    }

    private static DoughRecommendationViewModel MapRecommendation(
        GenerateDoughPrepRecommendationResponse recommendation,
        int historicalWeeksToUse)
    {
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
            SavedAtUtc = DateTime.UtcNow
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
}
