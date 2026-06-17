using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Responses.DoughUsage;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.DoughUsage;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)},{nameof(ApplicationRole.PizzaMaker)}")]
[Route("prep/dough-usage")]
public sealed class DoughUsageController : Controller
{
    private const string StatusTypeKey = "DoughUsageStatusType";
    private const string StatusMessageKey = "DoughUsageStatusMessage";

    private readonly IDoughUsageTraceManagementService _doughUsageTraceManagementService;
    private readonly IDoughUsageTraceReadService _doughUsageTraceReadService;

    public DoughUsageController(
        IDoughUsageTraceManagementService doughUsageTraceManagementService,
        IDoughUsageTraceReadService doughUsageTraceReadService)
    {
        _doughUsageTraceManagementService = doughUsageTraceManagementService;
        _doughUsageTraceReadService = doughUsageTraceReadService;
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        DateOnly? date,
        string? destination,
        Guid? traceId,
        CancellationToken cancellationToken = default)
    {
        var usageDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var form = new DoughUsageTraceFormViewModel
        {
            UsageDate = usageDate,
            Destination = NormalizeDestinationOrDefault(destination)
        };

        if (traceId.HasValue)
        {
            if (!CanManageTraces())
            {
                return Forbid();
            }

            var trace = await _doughUsageTraceReadService.GetByIdAsync(traceId.Value, cancellationToken);
            if (trace is null)
            {
                SetStatusMessage("danger", "The selected dough usage trace could not be found.");
                return RedirectToAction(nameof(Create), new { date = usageDate.ToString("yyyy-MM-dd"), destination = form.Destination });
            }

            form = MapForm(trace);
        }

        try
        {
            return View(await BuildCreatePageModelAsync(form, cancellationToken));
        }
        catch (Exception exception) when (TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
        {
            SetStatusMessage(statusType, statusMessage);
            return View(new DoughUsageEntryPageViewModel
            {
                UsageDate = form.UsageDate,
                CanManageTraces = CanManageTraces(),
                Form = form
            });
        }
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Form")] DoughUsageTraceFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        ValidateForm(form);

        var currentUserId = GetRequiredCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildCreatePageModelAsync(form, cancellationToken));
        }

        try
        {
            if (form.IsEdit && form.DoughUsageTraceId.HasValue)
            {
                if (!CanManageTraces())
                {
                    return Forbid();
                }

                await _doughUsageTraceManagementService.CorrectAsync(
                    new CorrectDoughUsageTraceRequest
                    {
                        DoughUsageTraceId = form.DoughUsageTraceId.Value,
                        UsageDate = form.UsageDate,
                        SourceDoughBatchQualityRecordId = form.SourceDoughBatchQualityRecordId,
                        Destination = form.Destination,
                        TrayCount = form.TrayCount,
                        Notes = form.Notes,
                        UpdatedByUserId = currentUserId
                    },
                    cancellationToken);

                SetStatusMessage("success", "Dough usage trace updated.");
            }
            else
            {
                await _doughUsageTraceManagementService.CreateAsync(
                    new CreateDoughUsageTraceRequest
                    {
                        UsageDate = form.UsageDate,
                        SourceDoughBatchQualityRecordId = form.SourceDoughBatchQualityRecordId,
                        Destination = form.Destination,
                        TrayCount = form.TrayCount,
                        Notes = form.Notes,
                        CreatedByUserId = currentUserId
                    },
                    cancellationToken);

                SetStatusMessage("success", "Dough usage trace saved.");
            }

            return RedirectToAction(nameof(Create), new
            {
                date = form.UsageDate.ToString("yyyy-MM-dd"),
                destination = form.Destination
            });
        }
        catch (Exception exception) when (TryHandleRecoverableException(exception, out _, out var statusMessage))
        {
            ModelState.AddModelError(string.Empty, statusMessage);
            return View(await BuildCreatePageModelAsync(form, cancellationToken));
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid id,
        DateOnly usageDate,
        string? destination,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetRequiredCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        try
        {
            await _doughUsageTraceManagementService.DeleteAsync(
                new DeleteDoughUsageTraceRequest
                {
                    DoughUsageTraceId = id,
                    DeletedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Dough usage trace deleted.");
        }
        catch (Exception exception) when (TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
        {
            SetStatusMessage(statusType, statusMessage);
        }

        return RedirectToAction(nameof(Create), new
        {
            date = usageDate.ToString("yyyy-MM-dd"),
            destination = NormalizeDestinationOrDefault(destination)
        });
    }

    [HttpGet("reball-planning")]
    public async Task<IActionResult> ReballPlanning(
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var referenceDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        try
        {
            var response = await _doughUsageTraceReadService.GetReballPlanningForDateAsync(
                new GetDoughReballPlanningRequest
                {
                    ReferenceDate = referenceDate
                },
                cancellationToken);

            return View(MapPlanningPage(response));
        }
        catch (Exception exception) when (TryHandleRecoverableException(exception, out var statusType, out var statusMessage))
        {
            SetStatusMessage(statusType, statusMessage);
            return View(new DoughReballPlanningPageViewModel
            {
                ReferenceDate = referenceDate
            });
        }
    }

    private async Task<DoughUsageEntryPageViewModel> BuildCreatePageModelAsync(
        DoughUsageTraceFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (form.UsageDate == default)
        {
            form.UsageDate = DateOnly.FromDateTime(DateTime.Today);
        }

        var destination = NormalizeDestinationOrDefault(form.Destination);
        form.Destination = destination;

        var availableSources = await _doughUsageTraceReadService.GetAvailableSourcesForDateAsync(
            new GetAvailableDoughSourcesRequest
            {
                UsageDate = form.UsageDate,
                Destination = destination
            },
            cancellationToken);

        var recentTraces = await _doughUsageTraceReadService.SearchAsync(
            new SearchDoughUsageTracesRequest
            {
                UsageDateFrom = form.UsageDate,
                UsageDateTo = form.UsageDate
            },
            cancellationToken);

        return new DoughUsageEntryPageViewModel
        {
            UsageDate = form.UsageDate,
            CanManageTraces = CanManageTraces(),
            Form = form,
            AvailableSources = availableSources
                .Select(source => new DoughUsageSourceCardViewModel
                {
                    SourceDoughBatchQualityRecordId = source.SourceDoughBatchQualityRecordId,
                    SourceDate = source.SourceDate,
                    SourceType = source.SourceType,
                    AgeDays = source.AgeDays,
                    OriginalBalls = source.OriginalBalls,
                    UsedBalls = source.UsedBalls,
                    RemainingBalls = source.RemainingBalls,
                    RecommendedAction = source.RecommendedAction,
                    IsPreferredSource = source.IsPreferredSource,
                    HasWarning = source.HasWarning,
                    WarningMessage = source.WarningMessage,
                    IsSelected = source.SourceDoughBatchQualityRecordId == form.SourceDoughBatchQualityRecordId
                })
                .ToArray(),
            RecentTraces = recentTraces
                .Select(trace => new DoughUsageTraceListItemViewModel
                {
                    Id = trace.Id,
                    UsageDate = trace.UsageDate,
                    SourceDate = trace.SourceDate,
                    SourceType = trace.SourceType,
                    Destination = trace.Destination,
                    TrayCount = trace.TrayCount,
                    BallsUsed = trace.BallsUsed,
                    Notes = trace.Notes,
                    CanManage = CanManageTraces()
                })
                .ToArray()
        };
    }

    private static DoughUsageTraceFormViewModel MapForm(DoughUsageTraceResponse trace)
    {
        return new DoughUsageTraceFormViewModel
        {
            DoughUsageTraceId = trace.Id,
            IsEdit = true,
            UsageDate = trace.UsageDate,
            Destination = trace.Destination,
            SourceDoughBatchQualityRecordId = trace.SourceDoughBatchQualityRecordId,
            TrayCount = trace.TrayCount,
            Notes = trace.Notes
        };
    }

    private static DoughReballPlanningPageViewModel MapPlanningPage(DoughReballPlanningResponse response)
    {
        return new DoughReballPlanningPageViewModel
        {
            ReferenceDate = response.ReferenceDate,
            MustUseFirstSources = response.MustUseFirstSources.Select(MapSourceCard).ToArray(),
            ReviewSources = response.ReviewSources.Select(MapSourceCard).ToArray(),
            ReballCandidates = response.ReballCandidates.Select(MapSourceCard).ToArray(),
            DiscardCandidates = response.DiscardCandidates.Select(MapSourceCard).ToArray()
        };
    }

    private static DoughUsageSourceCardViewModel MapSourceCard(DoughSourceRemainingResponse source)
    {
        return new DoughUsageSourceCardViewModel
        {
            SourceDoughBatchQualityRecordId = source.SourceDoughBatchQualityRecordId,
            SourceDate = source.SourceDate,
            SourceType = source.SourceType,
            AgeDays = source.AgeDays,
            OriginalBalls = source.OriginalBalls,
            UsedBalls = source.UsedBalls,
            RemainingBalls = source.RemainingBalls,
            RecommendedAction = source.RecommendedAction
        };
    }

    private void ValidateForm(DoughUsageTraceFormViewModel form)
    {
        if (form.UsageDate == default)
        {
            ModelState.AddModelError(nameof(form.UsageDate), "Choose the usage date.");
        }

        if (form.SourceDoughBatchQualityRecordId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(form.SourceDoughBatchQualityRecordId), "Choose the dough source that was used.");
        }

        if (form.TrayCount <= 0)
        {
            ModelState.AddModelError(nameof(form.TrayCount), "Tray count must be greater than zero.");
        }

        if (!DoughUsageDestinationExtensions.TryParse(form.Destination, out _))
        {
            ModelState.AddModelError(nameof(form.Destination), "Choose where the dough was used.");
        }
    }

    private bool CanManageTraces()
    {
        return User.IsInRole(nameof(ApplicationRole.Admin)) ||
            User.IsInRole(nameof(ApplicationRole.Manager));
    }

    private static string NormalizeDestinationOrDefault(string? destination)
    {
        return DoughUsageDestinationExtensions.TryParse(destination, out var parsedDestination)
            ? parsedDestination.ToString()
            : DoughUsageDestination.Restaurant.ToString();
    }

    private static bool TryHandleRecoverableException(
        Exception exception,
        out string statusType,
        out string statusMessage)
    {
        if (IsMissingDoughUsageSchemaException(exception))
        {
            statusType = "warning";
            statusMessage = "Dough usage tracking is not ready in this database yet. Apply the DoughUsageTrace migration, then reload this screen.";
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

    private static bool IsMissingDoughUsageSchemaException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 208 } sqlException &&
                sqlException.Message.Contains("DoughUsageTraces", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string? GetRequiredCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private void SetStatusMessage(string statusType, string message)
    {
        TempData[StatusTypeKey] = statusType;
        TempData[StatusMessageKey] = message;
    }
}
