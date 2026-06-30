using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.DoughClosing;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("prep/daily-closing")]
public sealed class DailyClosingController : Controller
{
    private const string StatusTypeKey = "DailyClosingStatusType";
    private const string StatusMessageKey = "DailyClosingStatusMessage";
    private const int DefaultHistoricalWeeksToUse = 8;

    private readonly IDailyDoughClosingManagementService _dailyDoughClosingManagementService;
    private readonly IDailyDoughClosingReadService _dailyDoughClosingReadService;

    public DailyClosingController(
        IDailyDoughClosingManagementService dailyDoughClosingManagementService,
        IDailyDoughClosingReadService dailyDoughClosingReadService)
    {
        _dailyDoughClosingManagementService = dailyDoughClosingManagementService;
        _dailyDoughClosingReadService = dailyDoughClosingReadService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        DateOnly? referenceDate,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var selectedReferenceDate = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);
        var model = await BuildIndexViewModelAsync(selectedReferenceDate, cancellationToken);
        return View(model);
    }

    [HttpGet("close")]
    public async Task<IActionResult> Close(
        DateOnly? closingDate,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var selectedDate = closingDate ?? DateOnly.FromDateTime(DateTime.Today);
        var weekSummary = await _dailyDoughClosingReadService.GetWeekSummaryAsync(
            new GetDailyClosingWeekSummaryRequest
            {
                ReferenceDate = selectedDate,
                HistoricalWeeksToUse = DefaultHistoricalWeeksToUse
            },
            cancellationToken);

        var day = weekSummary.Days.FirstOrDefault(item => item.Date == selectedDate);
        if (day?.IsClosed == true && day.DailyClosingId.HasValue)
        {
            return RedirectToAction(nameof(Edit), new { id = day.DailyClosingId.Value });
        }

        return View("Form", new DailyDoughClosingFormPageViewModel
        {
            Title = "Close Daily Dough Usage",
            Intro = "Record how many dough balls were actually used today and compare it against the forecast for this day.",
            Form = new DailyDoughClosingFormViewModel
            {
                ClosingDate = selectedDate,
                ForecastNeededBalls = day?.ForecastNeededBalls ?? 0
            }
        });
    }

    [HttpPost("close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(
        [Bind(Prefix = "Form")] DailyDoughClosingFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;
        model.IsEdit = false;

        if (!ModelState.IsValid)
        {
            return View("Form", BuildFormPageModel(
                "Close Daily Dough Usage",
                "Record how many dough balls were actually used today and compare it against the forecast for this day.",
                model));
        }

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _dailyDoughClosingManagementService.CreateDailyClosingAsync(
                new CreateDailyDoughClosingRequest
                {
                    ClosingDate = model.ClosingDate,
                    ForecastNeededBalls = model.ForecastNeededBalls,
                    ActualUsedBalls = model.ActualUsedBalls,
                    Notes = model.Notes,
                    ClosedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", $"Daily closing saved for {model.ClosingDate:dddd, MMM d}.");
            return RedirectToAction(nameof(Index), new { referenceDate = model.ClosingDate.ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            SetStatusMessage("danger", exception.Message);
            return View("Form", BuildFormPageModel(
                "Close Daily Dough Usage",
                "Record how many dough balls were actually used today and compare it against the forecast for this day.",
                model));
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var closing = await _dailyDoughClosingReadService.GetByIdAsync(id, cancellationToken);
        if (closing is null)
        {
            SetStatusMessage("danger", "The requested daily closing could not be found.");
            return RedirectToAction(nameof(Index));
        }

        return View("Form", BuildFormPageModel(
            "Correct Daily Dough Closing",
            "Adjust the recorded usage when the team reviews the real numbers after the first save.",
            MapForm(closing, isEdit: true)));
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        [Bind(Prefix = "Form")] DailyDoughClosingFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;
        model.IsEdit = true;
        model.DailyDoughClosingId = id;

        if (!ModelState.IsValid)
        {
            return View("Form", BuildFormPageModel(
                "Correct Daily Dough Closing",
                "Adjust the recorded usage when the team reviews the real numbers after the first save.",
                model));
        }

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _dailyDoughClosingManagementService.CorrectDailyClosingAsync(
                new CorrectDailyDoughClosingRequest
                {
                    DailyDoughClosingId = id,
                    ForecastNeededBalls = model.ForecastNeededBalls,
                    ActualUsedBalls = model.ActualUsedBalls,
                    Notes = model.Notes,
                    CorrectionNote = model.CorrectionNote,
                    CorrectedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", $"Daily closing corrected for {model.ClosingDate:dddd, MMM d}.");
            return RedirectToAction(nameof(Index), new { referenceDate = model.ClosingDate.ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            SetStatusMessage("danger", exception.Message);
            return View("Form", BuildFormPageModel(
                "Correct Daily Dough Closing",
                "Adjust the recorded usage when the team reviews the real numbers after the first save.",
                model));
        }
    }

    private async Task<DailyDoughClosingIndexViewModel> BuildIndexViewModelAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken)
    {
        var request = new GetDailyClosingWeekSummaryRequest
        {
            ReferenceDate = referenceDate,
            HistoricalWeeksToUse = DefaultHistoricalWeeksToUse
        };

        var weekSummary = await _dailyDoughClosingReadService.GetWeekSummaryAsync(request, cancellationToken);
        var insights = await _dailyDoughClosingReadService.GetOperationalInsightsAsync(request, cancellationToken);

        return new DailyDoughClosingIndexViewModel
        {
            ReferenceDate = referenceDate,
            WeekStartDate = weekSummary.WeekStartDate,
            WeekEndDate = weekSummary.WeekEndDate,
            TotalForecastBalls = weekSummary.TotalForecastBalls,
            TotalActualUsedBalls = weekSummary.TotalActualUsedBalls,
            AccumulatedVariance = weekSummary.AccumulatedVariance,
            AccumulatedSurplus = weekSummary.AccumulatedSurplus,
            AccumulatedShortage = weekSummary.AccumulatedShortage,
            ClosedDaysCount = weekSummary.ClosedDaysCount,
            ProjectedSurplus = insights.ProjectedSurplus,
            HasSurplusWarning = insights.HasSurplusWarning,
            HasShortageWarning = insights.HasShortageWarning,
            TotalTracedUsedBallsOnClosedDays = insights.TotalTracedUsedBallsOnClosedDays,
            TraceReconciliationDifferenceBalls = insights.TraceReconciliationDifferenceBalls,
            HasTraceReconciliationWarning = insights.HasTraceReconciliationWarning,
            TraceReconciliationMessage = insights.TraceReconciliationMessage,
            Recommendation = insights.Recommendation,
            Days = weekSummary.Days
                .Select(MapDayCard)
                .ToArray()
        };
    }

    private static DailyDoughClosingDayCardViewModel MapDayCard(DailyClosingWeekDayResponse day)
    {
        return new DailyDoughClosingDayCardViewModel
        {
            Date = day.Date,
            ForecastNeededBalls = day.ForecastNeededBalls,
            ActualUsedBalls = day.ActualUsedBalls,
            DailyVariance = day.DailyVariance,
            IsClosed = day.IsClosed,
            DailyClosingId = day.DailyClosingId,
            Notes = day.Notes,
            IsToday = day.IsToday,
            IsFuture = day.IsFuture
        };
    }

    private static DailyDoughClosingFormViewModel MapForm(DailyDoughClosingResponse closing, bool isEdit)
    {
        return new DailyDoughClosingFormViewModel
        {
            DailyDoughClosingId = closing.Id,
            IsEdit = isEdit,
            ClosingDate = closing.ClosingDate,
            ForecastNeededBalls = closing.ForecastNeededBalls,
            ActualUsedBalls = closing.ActualUsedBalls,
            Notes = closing.Notes,
            CorrectionNote = closing.CorrectionNote
        };
    }

    private static DailyDoughClosingFormPageViewModel BuildFormPageModel(
        string title,
        string intro,
        DailyDoughClosingFormViewModel form)
    {
        return new DailyDoughClosingFormPageViewModel
        {
            Title = title,
            Intro = intro,
            Form = form
        };
    }

    private string? GetRequiredCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private void SetStatusMessage(string type, string message)
    {
        TempData[StatusTypeKey] = type;
        TempData[StatusMessageKey] = message;
    }
}
