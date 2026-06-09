using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;
using ParlorPrediction.Mvc.Models.DoughClosing;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("prep/weekly-closing")]
public sealed class WeeklyClosingController : Controller
{
    private const string StatusTypeKey = "WeeklyClosingStatusType";
    private const string StatusMessageKey = "WeeklyClosingStatusMessage";

    private readonly IWeeklyDoughClosingManagementService _weeklyDoughClosingManagementService;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public WeeklyClosingController(
        IWeeklyDoughClosingManagementService weeklyDoughClosingManagementService,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _weeklyDoughClosingManagementService = weeklyDoughClosingManagementService;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        DateOnly? referenceDate,
        DateOnly? fromWeekStartDate,
        DateOnly? toWeekStartDate,
        CancellationToken cancellationToken = default)
    {
        var selectedReferenceDate = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);
        var model = await BuildIndexViewModelAsync(
            selectedReferenceDate,
            fromWeekStartDate,
            toWeekStartDate,
            cancellationToken);

        return View(model);
    }

    [HttpGet("create")]
    public IActionResult Create(DateOnly? weekStartDate)
    {
        var normalizedWeekStartDate = NormalizeOperationalWeekStart(
            weekStartDate ?? NormalizeOperationalWeekStart(DateOnly.FromDateTime(DateTime.Today)).AddDays(-7));

        return View("Form", new WeeklyDoughClosingFormPageViewModel
        {
            Title = "Weekly Dough Closing",
            Intro = "Record what really remained at the end of the operational week so the next week starts from real carryover instead of resetting to zero.",
            Form = new WeeklyDoughClosingFormViewModel
            {
                WeekStartDate = normalizedWeekStartDate
            }
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Form")] WeeklyDoughClosingFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.IsEdit = false;

        if (!ModelState.IsValid)
        {
            return View("Form", BuildFormPageModel(
                "Weekly Dough Closing",
                "Record what really remained at the end of the operational week so the next week starts from real carryover instead of resetting to zero.",
                model));
        }

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _weeklyDoughClosingManagementService.CreateWeeklyClosingAsync(
                new CreateWeeklyDoughClosingRequest
                {
                    WeekStartDate = model.WeekStartDate,
                    NeededBalls = model.NeededBalls,
                    ProducedBalls = model.ProducedBalls,
                    UsedBalls = model.UsedBalls,
                    LostBalls = model.LostBalls,
                    LeftoverReadyBalls = model.LeftoverReadyBalls,
                    LeftoverAttentionBalls = model.LeftoverAttentionBalls,
                    LeftoverMixedLoads = model.LeftoverMixedLoads,
                    Notes = model.Notes,
                    ClosedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Weekly dough closing saved.");
            return RedirectToAction(nameof(Index), new { referenceDate = model.WeekStartDate.AddDays(7).ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            SetStatusMessage("danger", exception.Message);
            return View("Form", BuildFormPageModel(
                "Weekly Dough Closing",
                "Record what really remained at the end of the operational week so the next week starts from real carryover instead of resetting to zero.",
                model));
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var closing = await FindClosingByIdAsync(id, cancellationToken);
        if (closing is null)
        {
            SetStatusMessage("danger", "The requested weekly closing could not be found.");
            return RedirectToAction(nameof(Index));
        }

        return View("Form", BuildFormPageModel(
            "Correct Weekly Dough Closing",
            "Adjust the closing when the team reviews the real leftovers, losses, or notes after the first save.",
            MapForm(closing, isEdit: true)));
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        [Bind(Prefix = "Form")] WeeklyDoughClosingFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.IsEdit = true;
        model.WeeklyDoughClosingId = id;

        if (!ModelState.IsValid)
        {
            return View("Form", BuildFormPageModel(
                "Correct Weekly Dough Closing",
                "Adjust the closing when the team reviews the real leftovers, losses, or notes after the first save.",
                model));
        }

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _weeklyDoughClosingManagementService.CorrectWeeklyClosingAsync(
                new CorrectWeeklyDoughClosingRequest
                {
                    WeeklyDoughClosingId = id,
                    NeededBalls = model.NeededBalls,
                    ProducedBalls = model.ProducedBalls,
                    UsedBalls = model.UsedBalls,
                    LostBalls = model.LostBalls,
                    LeftoverReadyBalls = model.LeftoverReadyBalls,
                    LeftoverAttentionBalls = model.LeftoverAttentionBalls,
                    LeftoverMixedLoads = model.LeftoverMixedLoads,
                    Notes = model.Notes,
                    CorrectionNote = model.CorrectionNote,
                    CorrectedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Weekly dough closing corrected.");
            return RedirectToAction(nameof(Index), new { referenceDate = model.WeekStartDate.AddDays(7).ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            SetStatusMessage("danger", exception.Message);
            return View("Form", BuildFormPageModel(
                "Correct Weekly Dough Closing",
                "Adjust the closing when the team reviews the real leftovers, losses, or notes after the first save.",
                model));
        }
    }

    private async Task<WeeklyDoughClosingIndexViewModel> BuildIndexViewModelAsync(
        DateOnly referenceDate,
        DateOnly? fromWeekStartDate,
        DateOnly? toWeekStartDate,
        CancellationToken cancellationToken)
    {
        var closings = await _weeklyDoughClosingReadService.GetWeeklyClosingsAsync(
            new GetWeeklyClosingsRequest
            {
                FromWeekStartDate = fromWeekStartDate,
                ToWeekStartDate = toWeekStartDate
            },
            cancellationToken);

        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = referenceDate
            },
            cancellationToken);

        return new WeeklyDoughClosingIndexViewModel
        {
            ReferenceDate = referenceDate,
            FromWeekStartDate = fromWeekStartDate,
            ToWeekStartDate = toWeekStartDate,
            CarryoverPreview = new WeeklyDoughCarryoverPreviewViewModel
            {
                ReferenceDate = referenceDate,
                TargetWeekStartDate = carryover.TargetWeekStartDate,
                TargetWeekEndDate = carryover.TargetWeekEndDate,
                HasClosingCarryover = carryover.HasClosingCarryover,
                SourceWeekStartDate = carryover.SourceWeekStartDate,
                SourceWeekEndDate = carryover.SourceWeekEndDate,
                CarryoverReadyBalls = carryover.CarryoverReadyBalls,
                CarryoverAttentionBalls = carryover.CarryoverAttentionBalls,
                CarryoverAvailableBalls = carryover.CarryoverAvailableBalls,
                MixedButNotBalledLoads = carryover.MixedButNotBalledLoads,
                MixedButNotBalledPotentialBalls = carryover.MixedButNotBalledLoads * DoughRules.StandardBatchBalls,
                PreviousWeekProducedBalls = carryover.PreviousWeekProducedBalls,
                PreviousWeekUsedBalls = carryover.PreviousWeekUsedBalls,
                PreviousWeekLostBalls = carryover.PreviousWeekLostBalls,
                ClosingNotes = carryover.ClosingNotes
            },
            Closings = closings
                .Select(MapListItem)
                .ToArray()
        };
    }

    private async Task<WeeklyDoughClosingResponse?> FindClosingByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var closings = await _weeklyDoughClosingReadService.GetWeeklyClosingsAsync(
            new GetWeeklyClosingsRequest(),
            cancellationToken);

        return closings.FirstOrDefault(item => item.Id == id);
    }

    private static WeeklyDoughClosingListItemViewModel MapListItem(WeeklyDoughClosingResponse closing)
    {
        return new WeeklyDoughClosingListItemViewModel
        {
            Id = closing.Id,
            WeekStartDate = closing.WeekStartDate,
            WeekEndDate = closing.WeekEndDate,
            NeededBalls = closing.NeededBalls,
            ProducedBalls = closing.ProducedBalls,
            UsedBalls = closing.UsedBalls,
            LostBalls = closing.LostBalls,
            LeftoverReadyBalls = closing.LeftoverReadyBalls,
            LeftoverAttentionBalls = closing.LeftoverAttentionBalls,
            LeftoverMixedLoads = closing.LeftoverMixedLoads,
            CarryoverAvailableBalls = closing.CarryoverAvailableBalls,
            Notes = closing.Notes,
            ClosedByUserId = closing.ClosedByUserId,
            ClosedAtUtc = closing.ClosedAtUtc,
            WasCorrected = closing.WasCorrected,
            CorrectedByUserId = closing.CorrectedByUserId,
            CorrectedAtUtc = closing.CorrectedAtUtc,
            CorrectionNote = closing.CorrectionNote
        };
    }

    private static WeeklyDoughClosingFormViewModel MapForm(WeeklyDoughClosingResponse closing, bool isEdit)
    {
        return new WeeklyDoughClosingFormViewModel
        {
            WeeklyDoughClosingId = closing.Id,
            IsEdit = isEdit,
            WeekStartDate = closing.WeekStartDate,
            NeededBalls = closing.NeededBalls,
            ProducedBalls = closing.ProducedBalls,
            UsedBalls = closing.UsedBalls,
            LostBalls = closing.LostBalls,
            LeftoverReadyBalls = closing.LeftoverReadyBalls,
            LeftoverAttentionBalls = closing.LeftoverAttentionBalls,
            LeftoverMixedLoads = closing.LeftoverMixedLoads,
            Notes = closing.Notes,
            CorrectionNote = closing.CorrectionNote
        };
    }

    private static WeeklyDoughClosingFormPageViewModel BuildFormPageModel(
        string title,
        string intro,
        WeeklyDoughClosingFormViewModel form)
    {
        return new WeeklyDoughClosingFormPageViewModel
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

    private static DateOnly NormalizeOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }
}
