using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.OperationalDrafts;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("operational-drafts")]
public sealed class OperationalDraftsController : Controller
{
    private const string StatusTypeKey = "OperationalDraftsStatusType";
    private const string StatusMessageKey = "OperationalDraftsStatusMessage";

    private readonly IOperationalDraftReadService _operationalDraftReadService;
    private readonly IOperationalDraftService _operationalDraftService;
    private readonly IOperationalPreviewService _operationalPreviewService;

    public OperationalDraftsController(
        IOperationalDraftReadService operationalDraftReadService,
        IOperationalDraftService operationalDraftService,
        IOperationalPreviewService operationalPreviewService)
    {
        _operationalDraftReadService = operationalDraftReadService;
        _operationalDraftService = operationalDraftService;
        _operationalPreviewService = operationalPreviewService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var inbox = await _operationalDraftReadService.GetInboxAsync(cancellationToken: cancellationToken);
        return View(OperationalDraftViewModelMapper.MapListPage(inbox));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var detail = await _operationalDraftReadService.GetDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            SetStatusMessage("danger", "The requested operational draft could not be found.");
            return RedirectToAction(nameof(Index));
        }

        return View(OperationalDraftViewModelMapper.MapDetails(detail));
    }

    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var detail = await _operationalDraftReadService.GetDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        var preview = await _operationalPreviewService.BuildPreviewAsync(id, cancellationToken);
        return PartialView(
            "_OperationalPreviewPanel",
            OperationalDraftViewModelMapper.MapPreviewPanel(detail.Draft, preview));
    }

    [HttpPost("{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            var preview = await _operationalPreviewService.BuildPreviewAsync(id, cancellationToken);
            if (preview.HasConflicts)
            {
                SetStatusMessage("danger", "Approval is blocked because the latest preview contains conflicts.");
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.Equals(preview.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
            {
                SetStatusMessage("danger", "Approval is blocked because the latest preview risk level is High.");
                return RedirectToAction(nameof(Details), new { id });
            }

            await _operationalDraftService.ApproveDraftAsync(id, currentUserId, cancellationToken);
            SetStatusMessage("success", "Operational draft approved and applied through the existing application services.");
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            SetStatusMessage("danger", exception.Message);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromForm] string reason,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            SetStatusMessage("danger", "A reason is required to reject or request correction for a draft.");
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _operationalDraftService.RejectDraftAsync(id, reason, currentUserId, cancellationToken);
            SetStatusMessage("success", "Operational draft rejected. The rejection reason was saved for audit.");
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            SetStatusMessage("danger", exception.Message);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private void SetStatusMessage(string type, string message)
    {
        TempData[StatusTypeKey] = type;
        TempData[StatusMessageKey] = message;
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is ArgumentException
            or InvalidOperationException
            or KeyNotFoundException;
    }
}
