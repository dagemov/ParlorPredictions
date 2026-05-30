using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.Dashboard;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("dashboard")]
public sealed class DashboardController : Controller
{
    private readonly IPrepDashboardReadService _prepDashboardReadService;

    public DashboardController(IPrepDashboardReadService prepDashboardReadService)
    {
        _prepDashboardReadService = prepDashboardReadService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        DateOnly? targetDate,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        var summary = await _prepDashboardReadService.GetSummaryAsync(selectedDate, cancellationToken);

        return View(new PrepDashboardViewModel
        {
            TargetDate = summary.TargetDate,
            HasRecommendation = summary.HasRecommendation,
            RequiredBalls = summary.RequiredBalls,
            AvailableBalls = summary.AvailableBalls,
            MissingBalls = summary.MissingBalls,
            RecommendedCases = summary.RecommendedCases,
            RecommendedLoads = summary.RecommendedLoads,
            PendingTasks = summary.PendingTasks,
            CompletedTasks = summary.CompletedTasks,
            LastRecommendationReason = summary.LastRecommendationReason,
            LastRecommendationSavedAtUtc = summary.LastRecommendationSavedAtUtc
        });
    }
}
