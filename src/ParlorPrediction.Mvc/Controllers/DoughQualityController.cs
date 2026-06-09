using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughQuality;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.DoughQuality;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)},{nameof(ApplicationRole.PizzaMaker)}")]
[Route("prep/dough-quality")]
public sealed class DoughQualityController : Controller
{
    private const string StatusTypeKey = "DoughQualityStatusType";
    private const string StatusMessageKey = "DoughQualityStatusMessage";

    private static readonly string[] ReviewStatusOptions =
    [
        nameof(DoughQualityStatus.Good),
        nameof(DoughQualityStatus.Attention),
        nameof(DoughQualityStatus.Reballed),
        nameof(DoughQualityStatus.MustUseNextDay),
        nameof(DoughQualityStatus.Discarded)
    ];

    private static readonly string[] CorrectableStatusOptions =
    [
        nameof(DoughQualityStatus.Good),
        nameof(DoughQualityStatus.Attention),
        nameof(DoughQualityStatus.MustUseNextDay),
        nameof(DoughQualityStatus.Discarded)
    ];

    private static readonly string[] LossReasonOptions =
    [
        nameof(DoughLossReason.TooHot),
        nameof(DoughLossReason.OverFermented),
        nameof(DoughLossReason.StoredTooManyDays),
        nameof(DoughLossReason.Contamination),
        nameof(DoughLossReason.FifoNotFollowed),
        nameof(DoughLossReason.NotSoldEnough),
        nameof(DoughLossReason.OverProduced),
        nameof(DoughLossReason.ManagerDecision),
        nameof(DoughLossReason.Other)
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

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpGet("loss-analytics")]
    public async Task<IActionResult> Losses(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? lossReason,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildLossAnalyticsFilter(fromDate, toDate, lossReason);

        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value > filter.ToDate.Value)
        {
            SetStatusMessage("danger", "The start date must be on or before the end date.");
            return View(BuildEmptyLossAnalyticsPageViewModel(filter));
        }

        try
        {
            var model = await BuildLossAnalyticsPageViewModelAsync(filter, cancellationToken);
            return View(model);
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            return View(BuildEmptyLossAnalyticsPageViewModel(filter));
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
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

        try
        {
            var model = await BuildReviewPageViewModelAsync(filter, cancellationToken);
            return View(model);
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            return View(BuildEmptyReviewPageViewModel(filter));
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
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

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
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

    [HttpGet("reball")]
    public async Task<IActionResult> Reball(
        Guid? recordId,
        DateOnly? referenceDate,
        CancellationToken cancellationToken = default)
    {
        var selectedReferenceDate = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);

        try
        {
            var model = await BuildReballPageViewModelAsync(selectedReferenceDate, recordId, null, cancellationToken);

            if (recordId.HasValue && model.SelectedRecord is null)
            {
                SetStatusMessage("danger", "The selected dough record is no longer available for reball.");
            }

            return View(model);
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            return View(BuildEmptyReballPageViewModel(selectedReferenceDate));
        }
    }

    [HttpPost("reball")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reball(
        [Bind(Prefix = "Form")]
        DoughQualityReballFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (model.DoughBatchQualityRecordId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.DoughBatchQualityRecordId), "Choose a dough record before saving reball.");
        }

        if (model.QuantityBeforeBalls > 0 && model.QuantityRecoveredBalls >= model.QuantityBeforeBalls)
        {
            ModelState.AddModelError(nameof(model.QuantityRecoveredBalls), "Recovered dough must stay lower than the quantity before reball.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildReballPageViewModelAsync(
                model.ReferenceDate,
                model.DoughBatchQualityRecordId == Guid.Empty ? null : model.DoughBatchQualityRecordId,
                model,
                cancellationToken);

            return View(invalidModel);
        }

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _doughQualityManagementService.ReballAsync(
                new ReballDoughRequest
                {
                    DoughBatchQualityRecordId = model.DoughBatchQualityRecordId,
                    QuantityRecoveredBalls = model.QuantityRecoveredBalls,
                    ReballDateUtc = BuildUtcDate(model.ReballDate),
                    Result = nameof(ReballResult.PartialRecovered),
                    DiscardReason = model.LossReason,
                    ManagerNote = model.ManagerNote,
                    UpdatedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Reball saved. The recovered dough now counts as Must Use Next Day.");
            return RedirectToAction(
                nameof(Reball),
                new
                {
                    recordId = model.DoughBatchQualityRecordId,
                    referenceDate = model.ReferenceDate.ToString("yyyy-MM-dd")
                });
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            var failedModel = await BuildReballPageViewModelAsync(
                model.ReferenceDate,
                model.DoughBatchQualityRecordId,
                model,
                cancellationToken);

            return View(failedModel);
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpGet("discard")]
    public async Task<IActionResult> Discard(
        Guid? recordId,
        DateOnly? referenceDate,
        CancellationToken cancellationToken = default)
    {
        var selectedReferenceDate = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);

        try
        {
            var model = await BuildDiscardPageViewModelAsync(selectedReferenceDate, recordId, null, cancellationToken);

            if (recordId.HasValue && model.SelectedRecord is null)
            {
                SetStatusMessage("danger", "The selected dough record is not available to discard.");
            }

            return View(model);
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            return View(BuildEmptyDiscardPageViewModel(selectedReferenceDate));
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("discard")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Discard(
        [Bind(Prefix = "Form")]
        DoughQualityDiscardFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (model.DoughBatchQualityRecordId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.DoughBatchQualityRecordId), "Choose a dough record before discarding it.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildDiscardPageViewModelAsync(
                model.ReferenceDate,
                model.DoughBatchQualityRecordId == Guid.Empty ? null : model.DoughBatchQualityRecordId,
                model,
                cancellationToken);

            return View(invalidModel);
        }

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _doughQualityManagementService.DiscardAsync(
                new DiscardDoughRequest
                {
                    DoughBatchQualityRecordId = model.DoughBatchQualityRecordId,
                    DiscardReason = model.DiscardReason,
                    DiscardedAtUtc = BuildUtcDate(model.DiscardDate),
                    ManagerNote = model.ManagerNote,
                    UpdatedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Discard saved. This dough no longer counts as available.");
            return RedirectToAction(
                nameof(Discard),
                new
                {
                    recordId = model.DoughBatchQualityRecordId,
                    referenceDate = model.ReferenceDate.ToString("yyyy-MM-dd")
                });
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            var failedModel = await BuildDiscardPageViewModelAsync(
                model.ReferenceDate,
                model.DoughBatchQualityRecordId,
                model,
                cancellationToken);

            return View(failedModel);
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)}")]
    [HttpGet("correct-status")]
    public async Task<IActionResult> CorrectStatus(
        Guid? recordId,
        DateOnly? referenceDate,
        CancellationToken cancellationToken = default)
    {
        var selectedReferenceDate = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);

        try
        {
            var model = await BuildCorrectStatusPageViewModelAsync(selectedReferenceDate, recordId, null, cancellationToken);

            if (recordId.HasValue && model.SelectedRecord is null)
            {
                SetStatusMessage("danger", "The selected dough record could not be found for correction.");
            }

            return View(model);
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            return View(BuildEmptyCorrectStatusPageViewModel(selectedReferenceDate));
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)}")]
    [HttpPost("correct-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CorrectStatus(
        [Bind(Prefix = "Form")]
        DoughQualityCorrectStatusFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (model.DoughBatchQualityRecordId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.DoughBatchQualityRecordId), "Choose a dough record before correcting its status.");
        }

        if (string.Equals(model.NewStatus, nameof(DoughQualityStatus.Discarded), StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(model.DiscardReason))
        {
            ModelState.AddModelError(nameof(model.DiscardReason), "Choose a discard reason when the corrected status is discarded.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildCorrectStatusPageViewModelAsync(
                model.ReferenceDate,
                model.DoughBatchQualityRecordId == Guid.Empty ? null : model.DoughBatchQualityRecordId,
                model,
                cancellationToken);

            return View(invalidModel);
        }

        var currentUserId = GetRequiredCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _doughQualityManagementService.CorrectStatusAsync(
                new CorrectDoughQualityStatusRequest
                {
                    DoughBatchQualityRecordId = model.DoughBatchQualityRecordId,
                    NewStatus = model.NewStatus,
                    StatusReason = model.StatusReason,
                    EffectiveAtUtc = BuildUtcDate(model.EffectiveDate),
                    MustUseByDate = string.Equals(model.NewStatus, nameof(DoughQualityStatus.MustUseNextDay), StringComparison.OrdinalIgnoreCase)
                        ? model.MustUseByDate
                        : null,
                    DiscardReason = string.Equals(model.NewStatus, nameof(DoughQualityStatus.Discarded), StringComparison.OrdinalIgnoreCase)
                        ? model.DiscardReason
                        : null,
                    ManagerNote = model.ManagerNote,
                    UpdatedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Status correction saved.");
            return RedirectToAction(
                nameof(CorrectStatus),
                new
                {
                    recordId = model.DoughBatchQualityRecordId,
                    referenceDate = model.ReferenceDate.ToString("yyyy-MM-dd")
                });
        }
        catch (Exception exception)
        {
            if (!TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
            {
                throw;
            }

            SetStatusMessage(statusType, statusMessage);
            var failedModel = await BuildCorrectStatusPageViewModelAsync(
                model.ReferenceDate,
                model.DoughBatchQualityRecordId,
                model,
                cancellationToken);

            return View(failedModel);
        }
    }

    private async Task<DoughQualityReviewPageViewModel> BuildReviewPageViewModelAsync(
        DoughQualityReviewFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryAsync(cancellationToken);
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
            Summary = summary,
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

    private async Task<DoughLossAnalyticsPageViewModel> BuildLossAnalyticsPageViewModelAsync(
        DoughLossAnalyticsFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var analytics = await _doughQualityReadService.GetLossAnalyticsAsync(
            new GetDoughLossAnalyticsRequest
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                LossReason = filter.LossReason
            },
            cancellationToken);

        var items = analytics.Items
            .OrderBy(item => item.LossDate)
            .ThenBy(item => item.LossReason, StringComparer.OrdinalIgnoreCase)
            .Select(item => new DoughLossAnalyticsItemViewModel
            {
                LossDate = item.LossDate,
                LossReason = item.LossReason,
                QuantityLostBalls = item.QuantityLostBalls
            })
            .ToArray();

        var reasonBreakdown = items
            .GroupBy(item => item.LossReason, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DoughLossAnalyticsReasonSummaryViewModel
            {
                LossReason = group.First().LossReason,
                QuantityLostBalls = group.Sum(item => item.QuantityLostBalls),
                SharePercent = analytics.TotalLostBalls <= 0
                    ? 0
                    : (int)Math.Round(group.Sum(item => item.QuantityLostBalls) * 100d / analytics.TotalLostBalls, MidpointRounding.AwayFromZero),
                LossDaysCount = group.Select(item => item.LossDate).Distinct().Count()
            })
            .OrderByDescending(item => item.QuantityLostBalls)
            .ThenBy(item => item.DisplayReason, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var dayBreakdown = items
            .GroupBy(item => item.LossDate)
            .Select(group =>
            {
                var topReason = group
                    .OrderByDescending(item => item.QuantityLostBalls)
                    .ThenBy(item => item.DisplayReason, StringComparer.OrdinalIgnoreCase)
                    .First();

                return new DoughLossAnalyticsDaySummaryViewModel
                {
                    LossDate = group.Key,
                    QuantityLostBalls = group.Sum(item => item.QuantityLostBalls),
                    TopReason = topReason.LossReason,
                    ReasonsCount = group.Count()
                };
            })
            .OrderBy(item => item.LossDate)
            .ToArray();

        var topReasonSummary = reasonBreakdown.FirstOrDefault();
        var topDaySummary = dayBreakdown
            .OrderByDescending(item => item.QuantityLostBalls)
            .ThenBy(item => item.LossDate)
            .FirstOrDefault();

        return new DoughLossAnalyticsPageViewModel
        {
            Filter = filter,
            LossReasonOptions = BuildLossReasonOptions(filter.LossReason, includeBlankOption: true, blankLabel: "All reasons"),
            Items = items,
            ReasonBreakdown = reasonBreakdown,
            DayBreakdown = dayBreakdown,
            TotalLostBalls = analytics.TotalLostBalls,
            TotalLossDays = dayBreakdown.Length,
            AverageLostBallsPerLossDay = dayBreakdown.Length == 0
                ? 0
                : (int)Math.Round(analytics.TotalLostBalls / (double)dayBreakdown.Length, MidpointRounding.AwayFromZero),
            MostCommonReasonLabel = topReasonSummary?.DisplayReason ?? "No losses yet",
            MostCommonReasonBalls = topReasonSummary?.QuantityLostBalls ?? 0,
            HighestLossDayLabel = topDaySummary is null
                ? "No loss day yet"
                : topDaySummary.LossDate.ToString("ddd, MMM d"),
            HighestLossDayBalls = topDaySummary?.QuantityLostBalls ?? 0,
            RangeSummary = BuildLossRangeSummary(filter),
            PrimaryInsight = BuildPrimaryLossInsight(filter, analytics.TotalLostBalls, topReasonSummary),
            SecondaryInsight = BuildSecondaryLossInsight(analytics.TotalLostBalls, topDaySummary),
            FutureFacingNote = "These loss patterns can support future recommendations."
        };
    }

    private async Task<DoughQualityReballPageViewModel> BuildReballPageViewModelAsync(
        DateOnly referenceDate,
        Guid? selectedRecordId,
        DoughQualityReballFormViewModel? form,
        CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryAsync(cancellationToken);
        var records = await LoadRecordOptionsAsync(includeDiscarded: false, cancellationToken);
        var selectedRecord = SelectRecord(records, selectedRecordId ?? form?.DoughBatchQualityRecordId);

        form ??= BuildDefaultReballForm(referenceDate, selectedRecord);
        form.ReferenceDate = referenceDate;

        if (selectedRecord is not null && form.DoughBatchQualityRecordId == Guid.Empty)
        {
            form.DoughBatchQualityRecordId = selectedRecord.Id;
        }

        if (selectedRecord is not null && form.QuantityBeforeBalls <= 0)
        {
            form.QuantityBeforeBalls = selectedRecord.QuantityBalls;
        }

        if (form.ReballDate == default)
        {
            form.ReballDate = referenceDate;
        }

        return new DoughQualityReballPageViewModel
        {
            ReferenceDate = referenceDate,
            Summary = summary,
            Form = form,
            SelectedRecord = selectedRecord,
            Records = records,
            LossReasonOptions = BuildLossReasonOptions(form.LossReason, includeBlankOption: true),
            IsManagerOrAdmin = User.IsInRole(nameof(ApplicationRole.Admin)) || User.IsInRole(nameof(ApplicationRole.Manager))
        };
    }

    private async Task<DoughQualityDiscardPageViewModel> BuildDiscardPageViewModelAsync(
        DateOnly referenceDate,
        Guid? selectedRecordId,
        DoughQualityDiscardFormViewModel? form,
        CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryAsync(cancellationToken);
        var records = await LoadRecordOptionsAsync(includeDiscarded: false, cancellationToken);
        var selectedRecord = SelectRecord(records, selectedRecordId ?? form?.DoughBatchQualityRecordId);

        form ??= BuildDefaultDiscardForm(referenceDate, selectedRecord);
        form.ReferenceDate = referenceDate;

        if (selectedRecord is not null && form.DoughBatchQualityRecordId == Guid.Empty)
        {
            form.DoughBatchQualityRecordId = selectedRecord.Id;
        }

        if (form.DiscardDate == default)
        {
            form.DiscardDate = referenceDate;
        }

        return new DoughQualityDiscardPageViewModel
        {
            ReferenceDate = referenceDate,
            Summary = summary,
            Form = form,
            SelectedRecord = selectedRecord,
            Records = records,
            LossReasonOptions = BuildLossReasonOptions(form.DiscardReason, includeBlankOption: true, blankLabel: "Choose a reason")
        };
    }

    private async Task<DoughQualityCorrectStatusPageViewModel> BuildCorrectStatusPageViewModelAsync(
        DateOnly referenceDate,
        Guid? selectedRecordId,
        DoughQualityCorrectStatusFormViewModel? form,
        CancellationToken cancellationToken)
    {
        var summary = await BuildSummaryAsync(cancellationToken);
        var records = await LoadRecordOptionsAsync(includeDiscarded: true, cancellationToken);
        var selectedRecord = SelectRecord(records, selectedRecordId ?? form?.DoughBatchQualityRecordId);

        form ??= BuildDefaultCorrectStatusForm(referenceDate, selectedRecord);
        form.ReferenceDate = referenceDate;

        if (selectedRecord is not null && form.DoughBatchQualityRecordId == Guid.Empty)
        {
            form.DoughBatchQualityRecordId = selectedRecord.Id;
        }

        if (form.EffectiveDate == default)
        {
            form.EffectiveDate = referenceDate;
        }

        return new DoughQualityCorrectStatusPageViewModel
        {
            ReferenceDate = referenceDate,
            Summary = summary,
            Form = form,
            SelectedRecord = selectedRecord,
            Records = records,
            StatusOptions = BuildCorrectStatusOptions(form.NewStatus),
            LossReasonOptions = BuildLossReasonOptions(form.DiscardReason, includeBlankOption: true)
        };
    }

    private async Task<DoughQualitySummaryViewModel> BuildSummaryAsync(CancellationToken cancellationToken)
    {
        var summary = await _doughQualityReadService.GetSummaryAsync(cancellationToken);

        return new DoughQualitySummaryViewModel
        {
            GoodBalls = summary.GoodBalls,
            AttentionBalls = summary.AttentionBalls,
            ReballedBalls = summary.ReballedBalls,
            MustUseNextDayBalls = summary.MustUseNextDayBalls,
            DiscardedBalls = summary.DiscardedBalls,
            TotalAvailableBalls = summary.TotalAvailableBalls
        };
    }

    private async Task<IReadOnlyList<DoughQualityReviewRecordViewModel>> LoadRecordOptionsAsync(
        bool includeDiscarded,
        CancellationToken cancellationToken)
    {
        var records = await _doughQualityReadService.SearchAsync(
            new SearchDoughBatchQualityRecordsRequest(),
            cancellationToken);

        return records
            .Where(record => includeDiscarded || !string.Equals(record.CurrentStatus, nameof(DoughQualityStatus.Discarded), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.CreatedOrBalledAt)
            .Select(MapRecord)
            .ToArray();
    }

    private DoughQualityReviewPageViewModel BuildEmptyReviewPageViewModel(DoughQualityReviewFilterViewModel filter)
    {
        return new DoughQualityReviewPageViewModel
        {
            Filter = filter,
            Summary = new DoughQualitySummaryViewModel(),
            StatusOptions = BuildStatusOptions(filter.CurrentStatus),
            CanCorrectStatus = User.IsInRole(nameof(ApplicationRole.Admin))
        };
    }

    private DoughLossAnalyticsPageViewModel BuildEmptyLossAnalyticsPageViewModel(DoughLossAnalyticsFilterViewModel filter)
    {
        return new DoughLossAnalyticsPageViewModel
        {
            Filter = filter,
            LossReasonOptions = BuildLossReasonOptions(filter.LossReason, includeBlankOption: true, blankLabel: "All reasons"),
            RangeSummary = BuildLossRangeSummary(filter),
            PrimaryInsight = "No dough losses were recorded for this range.",
            SecondaryInsight = "When losses appear, this screen will highlight the strongest pattern by day and reason."
        };
    }

    private DoughQualityReballPageViewModel BuildEmptyReballPageViewModel(DateOnly referenceDate)
    {
        return new DoughQualityReballPageViewModel
        {
            ReferenceDate = referenceDate,
            Summary = new DoughQualitySummaryViewModel(),
            Form = new DoughQualityReballFormViewModel
            {
                ReferenceDate = referenceDate,
                ReballDate = referenceDate
            },
            LossReasonOptions = BuildLossReasonOptions(null, includeBlankOption: true),
            IsManagerOrAdmin = User.IsInRole(nameof(ApplicationRole.Admin)) || User.IsInRole(nameof(ApplicationRole.Manager))
        };
    }

    private DoughQualityDiscardPageViewModel BuildEmptyDiscardPageViewModel(DateOnly referenceDate)
    {
        return new DoughQualityDiscardPageViewModel
        {
            ReferenceDate = referenceDate,
            Summary = new DoughQualitySummaryViewModel(),
            Form = new DoughQualityDiscardFormViewModel
            {
                ReferenceDate = referenceDate,
                DiscardDate = referenceDate
            },
            LossReasonOptions = BuildLossReasonOptions(null, includeBlankOption: true, blankLabel: "Choose a reason")
        };
    }

    private DoughQualityCorrectStatusPageViewModel BuildEmptyCorrectStatusPageViewModel(DateOnly referenceDate)
    {
        return new DoughQualityCorrectStatusPageViewModel
        {
            ReferenceDate = referenceDate,
            Summary = new DoughQualitySummaryViewModel(),
            Form = new DoughQualityCorrectStatusFormViewModel
            {
                ReferenceDate = referenceDate,
                EffectiveDate = referenceDate
            },
            StatusOptions = BuildCorrectStatusOptions(null),
            LossReasonOptions = BuildLossReasonOptions(null, includeBlankOption: true)
        };
    }

    private static DoughQualityReballFormViewModel BuildDefaultReballForm(
        DateOnly referenceDate,
        DoughQualityReviewRecordViewModel? selectedRecord)
    {
        return new DoughQualityReballFormViewModel
        {
            DoughBatchQualityRecordId = selectedRecord?.Id ?? Guid.Empty,
            ReferenceDate = referenceDate,
            QuantityBeforeBalls = selectedRecord?.QuantityBalls ?? 0,
            ReballDate = referenceDate
        };
    }

    private static DoughQualityDiscardFormViewModel BuildDefaultDiscardForm(
        DateOnly referenceDate,
        DoughQualityReviewRecordViewModel? selectedRecord)
    {
        return new DoughQualityDiscardFormViewModel
        {
            DoughBatchQualityRecordId = selectedRecord?.Id ?? Guid.Empty,
            ReferenceDate = referenceDate,
            DiscardDate = referenceDate
        };
    }

    private static DoughQualityCorrectStatusFormViewModel BuildDefaultCorrectStatusForm(
        DateOnly referenceDate,
        DoughQualityReviewRecordViewModel? selectedRecord)
    {
        var defaultStatus = string.Equals(selectedRecord?.CurrentStatus, nameof(DoughQualityStatus.Reballed), StringComparison.OrdinalIgnoreCase)
            ? nameof(DoughQualityStatus.MustUseNextDay)
            : selectedRecord?.CurrentStatus ?? nameof(DoughQualityStatus.Good);

        return new DoughQualityCorrectStatusFormViewModel
        {
            DoughBatchQualityRecordId = selectedRecord?.Id ?? Guid.Empty,
            ReferenceDate = referenceDate,
            NewStatus = defaultStatus,
            StatusReason = selectedRecord?.StatusReason,
            EffectiveDate = referenceDate,
            MustUseByDate = selectedRecord?.MustUseByDate ?? referenceDate.AddDays(1),
            DiscardReason = selectedRecord?.DiscardReason,
            ManagerNote = selectedRecord?.ManagerNote
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

    private string? GetRequiredCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static DateTime BuildUtcDate(DateOnly value)
    {
        var dateTime = value == default
            ? DateOnly.FromDateTime(DateTime.Today).ToDateTime(TimeOnly.MinValue)
            : value.ToDateTime(TimeOnly.MinValue);

        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    private static DoughQualityReviewRecordViewModel? SelectRecord(
        IReadOnlyList<DoughQualityReviewRecordViewModel> records,
        Guid? selectedRecordId)
    {
        if (!selectedRecordId.HasValue || selectedRecordId == Guid.Empty)
        {
            return null;
        }

        return records.FirstOrDefault(record => record.Id == selectedRecordId.Value);
    }

    private static IReadOnlyList<SelectListItem> BuildStatusOptions(string? selectedStatus)
    {
        return new[]
        {
            new SelectListItem("All statuses", string.Empty, string.IsNullOrWhiteSpace(selectedStatus))
        }.Concat(ReviewStatusOptions.Select(status => new SelectListItem(DoughQualityDisplayText.Format(status), status, string.Equals(status, selectedStatus, StringComparison.OrdinalIgnoreCase))))
            .ToArray();
    }

    private static IReadOnlyList<SelectListItem> BuildCorrectStatusOptions(string? selectedStatus)
    {
        return CorrectableStatusOptions
            .Select(status => new SelectListItem(DoughQualityDisplayText.Format(status), status, string.Equals(status, selectedStatus, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static IReadOnlyList<SelectListItem> BuildLossReasonOptions(
        string? selectedReason,
        bool includeBlankOption,
        string blankLabel = "No reason selected")
    {
        var options = LossReasonOptions
            .Select(reason => new SelectListItem(DoughQualityDisplayText.Format(reason), reason, string.Equals(reason, selectedReason, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (includeBlankOption)
        {
            options.Insert(0, new SelectListItem(blankLabel, string.Empty, string.IsNullOrWhiteSpace(selectedReason)));
        }

        return options;
    }

    private static DoughLossAnalyticsFilterViewModel BuildLossAnalyticsFilter(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? lossReason)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekStart = GetMondayOfWeek(today);

        return new DoughLossAnalyticsFilterViewModel
        {
            FromDate = fromDate ?? weekStart,
            ToDate = toDate ?? today,
            LossReason = string.IsNullOrWhiteSpace(lossReason) ? null : lossReason.Trim()
        };
    }

    private static string BuildLossRangeSummary(DoughLossAnalyticsFilterViewModel filter)
    {
        if (filter.FromDate.HasValue && filter.ToDate.HasValue)
        {
            return $"{filter.FromDate.Value:MMM d, yyyy} to {filter.ToDate.Value:MMM d, yyyy}";
        }

        if (filter.FromDate.HasValue)
        {
            return $"From {filter.FromDate.Value:MMM d, yyyy}";
        }

        if (filter.ToDate.HasValue)
        {
            return $"Through {filter.ToDate.Value:MMM d, yyyy}";
        }

        return "All recorded loss dates";
    }

    private static string BuildPrimaryLossInsight(
        DoughLossAnalyticsFilterViewModel filter,
        int totalLostBalls,
        DoughLossAnalyticsReasonSummaryViewModel? topReasonSummary)
    {
        if (totalLostBalls <= 0)
        {
            return "No dough losses were recorded for this range.";
        }

        if (!string.IsNullOrWhiteSpace(filter.LossReason))
        {
            var displayReason = DoughQualityDisplayText.Format(filter.LossReason);
            return $"{displayReason} caused {totalLostBalls} lost balls in this range.";
        }

        if (topReasonSummary is not null)
        {
            return $"Most losses came from {topReasonSummary.DisplayReason} in this range.";
        }

        return "Losses were recorded in this range. Start with the biggest reason first.";
    }

    private static string BuildSecondaryLossInsight(
        int totalLostBalls,
        DoughLossAnalyticsDaySummaryViewModel? topDaySummary)
    {
        if (totalLostBalls <= 0 || topDaySummary is null)
        {
            return "When losses appear, this screen will highlight the strongest pattern by day and reason.";
        }

        return topDaySummary.ReasonsCount > 1
            ? $"{topDaySummary.LossDate:dddd} had the highest loss total with {topDaySummary.QuantityLostBalls} balls across {topDaySummary.ReasonsCount} reasons."
            : $"{topDaySummary.LossDate:dddd} had the highest loss total with {topDaySummary.QuantityLostBalls} lost balls.";
    }

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
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
                (sqlException.Message.Contains("DoughBatchQualityRecords", StringComparison.OrdinalIgnoreCase) ||
                 sqlException.Message.Contains("DoughLossRecords", StringComparison.OrdinalIgnoreCase) ||
                 sqlException.Message.Contains("DoughReballRecords", StringComparison.OrdinalIgnoreCase)))
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
