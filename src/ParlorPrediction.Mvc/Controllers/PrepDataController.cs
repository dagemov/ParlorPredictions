using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.PrepData;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("prep/data")]
public sealed class PrepDataController : Controller
{
    private const string StatusTypeKey = "PrepDataStatusType";
    private const string StatusMessageKey = "PrepDataStatusMessage";

    private readonly IDoughDemandPlanService _doughDemandPlanService;
    private readonly IRestaurantEventManagementService _restaurantEventManagementService;

    public PrepDataController(
        IDoughDemandPlanService doughDemandPlanService,
        IRestaurantEventManagementService restaurantEventManagementService)
    {
        _doughDemandPlanService = doughDemandPlanService;
        _restaurantEventManagementService = restaurantEventManagementService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("dough-demand")]
    public async Task<IActionResult> DoughDemand(
        DayOfWeek? dayOfWeek,
        string? sourceTerm,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var model = await BuildDoughDemandPlanListPageViewModelAsync(
            dayOfWeek,
            sourceTerm,
            activeOnly,
            cancellationToken);

        if (IsHtmxRequest())
        {
            return PartialView("_DoughDemandPlanListPartial", model);
        }

        return View(model);
    }

    [HttpGet("dough-demand/create")]
    public IActionResult CreateDoughDemand()
    {
        return View(
            "DoughDemandPlanForm",
            BuildDoughDemandPlanFormViewModel(new DoughDemandPlanFormViewModel
            {
                IsActive = true
            }));
    }

    [HttpPost("dough-demand/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDoughDemand(
        DoughDemandPlanFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View("DoughDemandPlanForm", BuildDoughDemandPlanFormViewModel(model));
        }

        try
        {
            await _doughDemandPlanService.CreateAsync(
                MapDemandPlanRequest(model),
                cancellationToken);

            SetStatusMessage("success", "Dough demand plan created.");
            return RedirectToAction(nameof(DoughDemand));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception, "The dough demand plan could not be created."));
            return View("DoughDemandPlanForm", BuildDoughDemandPlanFormViewModel(model));
        }
    }

    [HttpGet("dough-demand/{id:guid}/edit")]
    public async Task<IActionResult> EditDoughDemand(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var demandPlan = await _doughDemandPlanService.GetByIdAsync(id, cancellationToken);
        if (demandPlan is null)
        {
            return NotFound();
        }

        return View(
            "DoughDemandPlanForm",
            BuildDoughDemandPlanFormViewModel(new DoughDemandPlanFormViewModel
            {
                Id = demandPlan.Id,
                DayOfWeek = demandPlan.DayOfWeek,
                SourceName = demandPlan.SourceName,
                MinDoughBalls = demandPlan.MinDoughBalls,
                MaxDoughBalls = demandPlan.MaxDoughBalls,
                Notes = demandPlan.Notes,
                IsActive = demandPlan.IsActive
            }));
    }

    [HttpPost("dough-demand/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDoughDemand(
        Guid id,
        DoughDemandPlanFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View("DoughDemandPlanForm", BuildDoughDemandPlanFormViewModel(model));
        }

        try
        {
            await _doughDemandPlanService.UpdateAsync(
                id,
                MapDemandPlanRequest(model),
                cancellationToken);

            SetStatusMessage("success", "Dough demand plan updated.");
            return RedirectToAction(nameof(DoughDemand));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception, "The dough demand plan could not be updated."));
            return View("DoughDemandPlanForm", BuildDoughDemandPlanFormViewModel(model));
        }
    }

    [HttpPost("dough-demand/{id:guid}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDoughDemand(
        Guid id,
        DayOfWeek? dayOfWeek,
        string? sourceTerm,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var demandPlan = await _doughDemandPlanService.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("The dough demand plan could not be found.");

            var nextActiveState = !demandPlan.IsActive;
            await _doughDemandPlanService.SetActiveAsync(id, nextActiveState, cancellationToken);

            var statusMessage = nextActiveState
                ? "Dough demand plan reactivated."
                : "Dough demand plan deactivated.";

            if (IsHtmxRequest())
            {
                TriggerHtmxAlert("success", "Demand plan updated", statusMessage);

                var partialModel = await BuildDoughDemandPlanListPageViewModelAsync(
                    dayOfWeek,
                    sourceTerm,
                    activeOnly,
                    cancellationToken);

                return PartialView("_DoughDemandPlanListPartial", partialModel);
            }

            SetStatusMessage("success", statusMessage);
        }
        catch (Exception exception)
        {
            var message = GetFriendlyErrorMessage(exception, "The dough demand plan could not be updated.");

            if (IsHtmxRequest())
            {
                TriggerHtmxAlert("error", "Demand plan update failed", message);

                var partialModel = await BuildDoughDemandPlanListPageViewModelAsync(
                    dayOfWeek,
                    sourceTerm,
                    activeOnly,
                    cancellationToken);

                return PartialView("_DoughDemandPlanListPartial", partialModel);
            }

            SetStatusMessage("danger", message);
        }

        return RedirectToAction(nameof(DoughDemand), new { dayOfWeek, sourceTerm, activeOnly });
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? term,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var model = await BuildRestaurantEventListPageViewModelAsync(
            fromDate,
            toDate,
            term,
            activeOnly,
            cancellationToken);

        if (IsHtmxRequest())
        {
            return PartialView("_RestaurantEventListPartial", model);
        }

        return View(model);
    }

    [HttpGet("events/create")]
    public IActionResult CreateEvent()
    {
        return View(
            "RestaurantEventForm",
            new RestaurantEventFormViewModel
            {
                EventDate = DateOnly.FromDateTime(DateTime.Today),
                IsActive = true
            });
    }

    [HttpPost("events/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(
        RestaurantEventFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View("RestaurantEventForm", model);
        }

        try
        {
            await _restaurantEventManagementService.CreateAsync(
                MapRestaurantEventRequest(model),
                cancellationToken);

            SetStatusMessage("success", "Restaurant event created.");
            return RedirectToAction(nameof(Events));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception, "The restaurant event could not be created."));
            return View("RestaurantEventForm", model);
        }
    }

    [HttpGet("events/{id:guid}/edit")]
    public async Task<IActionResult> EditEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var restaurantEvent = await _restaurantEventManagementService.GetByIdAsync(id, cancellationToken);
        if (restaurantEvent is null)
        {
            return NotFound();
        }

        return View(
            "RestaurantEventForm",
            new RestaurantEventFormViewModel
            {
                Id = restaurantEvent.Id,
                Name = restaurantEvent.Name,
                EventDate = restaurantEvent.EventDate,
                EstimatedPizzas = restaurantEvent.EstimatedPizzas,
                EstimatedDoughBalls = restaurantEvent.EstimatedDoughBalls,
                AllowShortFermentation = restaurantEvent.AllowShortFermentation,
                Notes = restaurantEvent.Notes,
                IsActive = restaurantEvent.IsActive
            });
    }

    [HttpPost("events/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(
        Guid id,
        RestaurantEventFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View("RestaurantEventForm", model);
        }

        try
        {
            await _restaurantEventManagementService.UpdateAsync(
                id,
                MapRestaurantEventRequest(model),
                cancellationToken);

            SetStatusMessage("success", "Restaurant event updated.");
            return RedirectToAction(nameof(Events));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception, "The restaurant event could not be updated."));
            return View("RestaurantEventForm", model);
        }
    }

    [HttpPost("events/{id:guid}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEvent(
        Guid id,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? term,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var restaurantEvent = await _restaurantEventManagementService.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("The restaurant event could not be found.");

            var nextActiveState = !restaurantEvent.IsActive;
            await _restaurantEventManagementService.SetActiveAsync(id, nextActiveState, cancellationToken);

            var statusMessage = nextActiveState
                ? "Restaurant event reactivated."
                : "Restaurant event deactivated.";

            if (IsHtmxRequest())
            {
                TriggerHtmxAlert("success", "Event updated", statusMessage);

                var partialModel = await BuildRestaurantEventListPageViewModelAsync(
                    fromDate,
                    toDate,
                    term,
                    activeOnly,
                    cancellationToken);

                return PartialView("_RestaurantEventListPartial", partialModel);
            }

            SetStatusMessage("success", statusMessage);
        }
        catch (Exception exception)
        {
            var message = GetFriendlyErrorMessage(exception, "The restaurant event could not be updated.");

            if (IsHtmxRequest())
            {
                TriggerHtmxAlert("error", "Event update failed", message);

                var partialModel = await BuildRestaurantEventListPageViewModelAsync(
                    fromDate,
                    toDate,
                    term,
                    activeOnly,
                    cancellationToken);

                return PartialView("_RestaurantEventListPartial", partialModel);
            }

            SetStatusMessage("danger", message);
        }

        return RedirectToAction(nameof(Events), new { fromDate, toDate, term, activeOnly });
    }

    private async Task<DoughDemandPlanListPageViewModel> BuildDoughDemandPlanListPageViewModelAsync(
        DayOfWeek? dayOfWeek,
        string? sourceTerm,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var demandPlans = await _doughDemandPlanService.SearchAsync(
            dayOfWeek,
            sourceTerm,
            activeOnly,
            cancellationToken);

        return new DoughDemandPlanListPageViewModel
        {
            DayOfWeek = dayOfWeek,
            SourceTerm = sourceTerm,
            ActiveOnly = activeOnly,
            DayOfWeekOptions = BuildDayOfWeekOptions(dayOfWeek, includeAllOption: true),
            DemandPlans = demandPlans.Select(MapDemandPlan).ToArray(),
            StatusType = ReadStatusType(),
            StatusMessage = ReadStatusMessage()
        };
    }

    private async Task<RestaurantEventListPageViewModel> BuildRestaurantEventListPageViewModelAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? term,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var restaurantEvents = await _restaurantEventManagementService.SearchAsync(
            fromDate,
            toDate,
            term,
            activeOnly,
            cancellationToken);

        return new RestaurantEventListPageViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Term = term,
            ActiveOnly = activeOnly,
            Events = restaurantEvents.Select(MapRestaurantEvent).ToArray(),
            StatusType = ReadStatusType(),
            StatusMessage = ReadStatusMessage()
        };
    }

    private DoughDemandPlanFormViewModel BuildDoughDemandPlanFormViewModel(DoughDemandPlanFormViewModel model)
    {
        model.DayOfWeekOptions = BuildDayOfWeekOptions(model.DayOfWeek, includeAllOption: false);
        return model;
    }

    private static DoughDemandPlanListItemViewModel MapDemandPlan(DoughDemandPlanListItemResponse response)
    {
        return new DoughDemandPlanListItemViewModel
        {
            Id = response.Id,
            DayOfWeek = response.DayOfWeek,
            SourceName = response.SourceName,
            MinDoughBalls = response.MinDoughBalls,
            MaxDoughBalls = response.MaxDoughBalls,
            BaselineDoughBalls = response.BaselineDoughBalls,
            Notes = response.Notes,
            IsActive = response.IsActive,
            UpdatedAtUtc = response.UpdatedAtUtc
        };
    }

    private static RestaurantEventListItemViewModel MapRestaurantEvent(RestaurantEventListItemResponse response)
    {
        return new RestaurantEventListItemViewModel
        {
            Id = response.Id,
            Name = response.Name,
            EventDate = response.EventDate,
            EstimatedPizzas = response.EstimatedPizzas,
            EstimatedDoughBalls = response.EstimatedDoughBalls,
            AllowShortFermentation = response.AllowShortFermentation,
            Notes = response.Notes,
            IsActive = response.IsActive,
            UpdatedAtUtc = response.UpdatedAtUtc
        };
    }

    private static SaveDoughDemandPlanRequest MapDemandPlanRequest(DoughDemandPlanFormViewModel model)
    {
        return new SaveDoughDemandPlanRequest
        {
            DayOfWeek = model.DayOfWeek,
            SourceName = model.SourceName,
            MinDoughBalls = model.MinDoughBalls,
            MaxDoughBalls = model.MaxDoughBalls,
            Notes = model.Notes,
            IsActive = model.IsActive
        };
    }

    private static SaveRestaurantEventRequest MapRestaurantEventRequest(RestaurantEventFormViewModel model)
    {
        return new SaveRestaurantEventRequest
        {
            Name = model.Name,
            EventDate = model.EventDate,
            EstimatedPizzas = model.EstimatedPizzas,
            EstimatedDoughBalls = model.EstimatedDoughBalls,
            AllowShortFermentation = model.AllowShortFermentation,
            Notes = model.Notes,
            IsActive = model.IsActive
        };
    }

    private static IReadOnlyList<SelectListItem> BuildDayOfWeekOptions(
        DayOfWeek? selectedDayOfWeek,
        bool includeAllOption)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("All days", string.Empty, !selectedDayOfWeek.HasValue));
        }

        items.AddRange(
            Enum.GetValues<DayOfWeek>()
                .Select(dayOfWeek => new SelectListItem(
                    dayOfWeek.ToString(),
                    dayOfWeek.ToString(),
                    selectedDayOfWeek == dayOfWeek)));

        return items;
    }

    private bool IsHtmxRequest()
    {
        return Request.Headers.TryGetValue("HX-Request", out var headerValue) &&
            string.Equals(headerValue.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private void TriggerHtmxAlert(string type, string title, string message)
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

    private void SetStatusMessage(string statusType, string message)
    {
        TempData[StatusTypeKey] = statusType;
        TempData[StatusMessageKey] = message;
    }

    private string? ReadStatusType()
    {
        return TempData[StatusTypeKey] as string;
    }

    private string? ReadStatusMessage()
    {
        return TempData[StatusMessageKey] as string;
    }

    private static string GetFriendlyErrorMessage(Exception exception, string fallbackMessage)
    {
        return exception switch
        {
            ArgumentOutOfRangeException => exception.Message,
            ArgumentException => exception.Message,
            InvalidOperationException => exception.Message,
            KeyNotFoundException => exception.Message,
            _ => fallbackMessage
        };
    }
}
