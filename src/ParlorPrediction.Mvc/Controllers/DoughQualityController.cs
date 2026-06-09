using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughQuality;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.DoughQuality;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("prep/dough-quality")]
public sealed class DoughQualityController : Controller
{
    private const string StatusTypeKey = "DoughQualityStatusType";
    private const string StatusMessageKey = "DoughQualityStatusMessage";

    private static readonly string[] StatusOptions =
    [
        nameof(DoughQualityStatus.Good),
        nameof(DoughQualityStatus.Attention),
        nameof(DoughQualityStatus.Reballed),
        nameof(DoughQualityStatus.MustUseNextDay),
        nameof(DoughQualityStatus.Discarded)
    ];

    private readonly IDoughQualityManagementService _doughQualityManagementService;
    private readonly IDoughQualityReadService _doughQualityReadService;

    public DoughQualityController(
        IDoughQualityReadService doughQualityReadService,
        IDoughQualityManagementService doughQualityManagementService)
    {
        _doughQualityReadService = doughQualityReadService;
        _doughQualityManagementService = doughQualityManagementService;
    }

    [HttpGet("review")]
    public async Task<IActionResult> Review(
        DateOnly? referenceDate,
        DateOnly? createdOrBalledFromDate,
        DateOnly? createdOrBalledToDate,
        DateOnly? reballedFromDate,
        DateOnly? reballedToDate,
        string? currentStatus,
        CancellationToken cancellationToken = default)
    {
        var filter = new DoughQualityReviewFilterViewModel
        {
            ReferenceDate = referenceDate ?? DateOnly.FromDateTime(DateTime.Today),
            CreatedOrBalledFromDate = createdOrBalledFromDate,
            CreatedOrBalledToDate = createdOrBalledToDate,
            ReballedFromDate = reballedFromDate,
            ReballedToDate = reballedToDate,
            CurrentStatus = currentStatus
        };

        DoughQualityReviewPageViewModel model;
        try
        {
            model = await BuildReviewPageViewModelAsync(filter, cancellationToken);
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            model = BuildEmptyReviewPageViewModel(filter);
        }

        return View(model);
    }

    [HttpPost("mark-attention")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAttention(
        MarkAttentionFromReviewViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (model.DoughBatchQualityRecordId == Guid.Empty)
        {
            SetStatusMessage("danger", "Choose a dough record before marking attention.");
            return RedirectToReview(model);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        try
        {
            await _doughQualityManagementService.MarkAsAttentionAsync(
                new MarkDoughAsAttentionRequest
                {
                    DoughBatchQualityRecordId = model.DoughBatchQualityRecordId,
                    StatusReason = model.StatusReason,
                    UpdatedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "The dough batch is now marked as attention and still counts as available.");
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
        }

        return RedirectToReview(model);
    }

    private async Task<DoughQualityReviewPageViewModel> BuildReviewPageViewModelAsync(
        DoughQualityReviewFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var records = await _doughQualityReadService.SearchAsync(
            new SearchDoughBatchQualityRecordsRequest
            {
                CreatedOrBalledFromDate = filter.CreatedOrBalledFromDate,
                CreatedOrBalledToDate = filter.CreatedOrBalledToDate,
                ReballedFromDate = filter.ReballedFromDate,
                ReballedToDate = filter.ReballedToDate,
                CurrentStatus = filter.CurrentStatus
            },
            cancellationToken);

        var recordIds = records.Select(record => record.Id).ToHashSet();
        var candidates = await _doughQualityReadService.EvaluateAttentionCandidatesAsync(
            new EvaluateDoughAttentionCandidatesRequest
            {
                ReferenceDate = filter.ReferenceDate
            },
            cancellationToken);

        var filteredCandidates = recordIds.Count == 0 && !HasActiveSearchFilters(filter)
            ? candidates
            : candidates.Where(candidate => recordIds.Contains(candidate.DoughBatchQualityRecordId)).ToArray();

        return new DoughQualityReviewPageViewModel
        {
            Filter = filter,
            StatusOptions = BuildStatusOptions(filter.CurrentStatus),
            AttentionCandidates = filteredCandidates
                .Select(MapCandidate)
                .ToArray(),
            Records = records
                .OrderByDescending(record => record.CreatedOrBalledAt)
                .Select(MapRecord)
                .ToArray(),
            CanCorrectStatus = User.IsInRole(nameof(ApplicationRole.Admin))
        };
    }

    private DoughQualityReviewPageViewModel BuildEmptyReviewPageViewModel(DoughQualityReviewFilterViewModel filter)
    {
        return new DoughQualityReviewPageViewModel
        {
            Filter = filter,
            StatusOptions = BuildStatusOptions(filter.CurrentStatus),
            CanCorrectStatus = User.IsInRole(nameof(ApplicationRole.Admin))
        };
    }

    private IActionResult RedirectToReview(MarkAttentionFromReviewViewModel model)
    {
        return RedirectToAction(
            nameof(Review),
            new
            {
                referenceDate = model.ReferenceDate.ToString("yyyy-MM-dd"),
                createdOrBalledFromDate = model.CreatedOrBalledFromDate?.ToString("yyyy-MM-dd"),
                createdOrBalledToDate = model.CreatedOrBalledToDate?.ToString("yyyy-MM-dd"),
                reballedFromDate = model.ReballedFromDate?.ToString("yyyy-MM-dd"),
                reballedToDate = model.ReballedToDate?.ToString("yyyy-MM-dd"),
                currentStatus = model.CurrentStatus
            });
    }

    private static IReadOnlyList<SelectListItem> BuildStatusOptions(string? selectedStatus)
    {
        return new[]
        {
            new SelectListItem("All statuses", string.Empty, string.IsNullOrWhiteSpace(selectedStatus))
        }.Concat(StatusOptions.Select(status => new SelectListItem(status, status, string.Equals(status, selectedStatus, StringComparison.OrdinalIgnoreCase))))
            .ToArray();
    }

    private static DoughQualityReviewCandidateViewModel MapCandidate(DoughAttentionCandidateResponse candidate)
    {
        return new DoughQualityReviewCandidateViewModel
        {
            DoughBatchQualityRecordId = candidate.DoughBatchQualityRecordId,
            SourceDate = candidate.SourceDate,
            CreatedOrBalledAt = candidate.CreatedOrBalledAt,
            QuantityBalls = candidate.QuantityBalls,
            CurrentStatus = candidate.CurrentStatus,
            AgeDays = candidate.AgeDays,
            CandidateReason = candidate.CandidateReason
        };
    }

    private static DoughQualityReviewRecordViewModel MapRecord(DoughBatchQualityRecordResponse record)
    {
        return new DoughQualityReviewRecordViewModel
        {
            Id = record.Id,
            SourceDate = record.SourceDate,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            QuantityBalls = record.QuantityBalls,
            CurrentStatus = record.CurrentStatus,
            StatusReason = record.StatusReason,
            AttentionMarkedAt = record.AttentionMarkedAt,
            ReballedAt = record.ReballedAt,
            MustUseByDate = record.MustUseByDate,
            DiscardedAt = record.DiscardedAt,
            DiscardReason = record.DiscardReason,
            ManagerNote = record.ManagerNote,
            CountsAsAvailable = record.CountsAsAvailable
        };
    }

    private static bool HasActiveSearchFilters(DoughQualityReviewFilterViewModel filter)
    {
        return filter.CreatedOrBalledFromDate.HasValue ||
            filter.CreatedOrBalledToDate.HasValue ||
            filter.ReballedFromDate.HasValue ||
            filter.ReballedToDate.HasValue ||
            !string.IsNullOrWhiteSpace(filter.CurrentStatus);
    }

    private static bool TryHandleRecoverableException(
        Exception exception,
        out string statusType,
        out string statusMessage)
    {
        if (IsMissingDoughQualitySchemaException(exception))
        {
            statusType = "warning";
            statusMessage = "Dough Quality tracking is not ready in this database yet. Apply the AddDoughQualityTracking migration, then reload this screen.";
            return true;
        }

        if (exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException or
            KeyNotFoundException)
        {
            statusType = "danger";
            statusMessage = exception.Message;
            return true;
        }

        statusType = string.Empty;
        statusMessage = string.Empty;
        return false;
    }

    private static bool IsMissingDoughQualitySchemaException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 208 } sqlException &&
                sqlException.Message.Contains("DoughBatchQualityRecords", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void SetStatusMessage(string statusType, string message)
    {
        TempData[StatusTypeKey] = statusType;
        TempData[StatusMessageKey] = message;
    }
}
