using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Ai;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.Dashboard;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("dashboard")]
public sealed class DashboardController : Controller
{
    private readonly IAiPrepRecommendationService _aiPrepRecommendationService;
    private readonly IPrepDashboardReadService _prepDashboardReadService;

    public DashboardController(
        IPrepDashboardReadService prepDashboardReadService,
        IAiPrepRecommendationService aiPrepRecommendationService)
    {
        _prepDashboardReadService = prepDashboardReadService;
        _aiPrepRecommendationService = aiPrepRecommendationService;
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
            WeeklyWindowEndDate = summary.WeeklyWindowEndDate,
            HasRecommendation = summary.HasRecommendation,
            RequiredBalls = summary.RequiredBalls,
            AvailableBalls = summary.AvailableBalls,
            MissingBalls = summary.MissingBalls,
            RecommendedCases = summary.RecommendedCases,
            RecommendedLoads = summary.RecommendedLoads,
            PendingTasks = summary.PendingTasks,
            CompletedTasks = summary.CompletedTasks,
            WeeklyNeededBalls = summary.WeeklyNeededBalls,
            WeeklyCoveredBalls = summary.WeeklyCoveredBalls,
            WeeklyPendingBalls = summary.WeeklyPendingBalls,
            WeeklyCompletedTasks = summary.WeeklyCompletedTasks,
            WeeklyPendingTasks = summary.WeeklyPendingTasks,
            WeeklyUpcomingEventBalls = summary.WeeklyUpcomingEventBalls,
            LastRecommendationReason = summary.LastRecommendationReason,
            LastRecommendationSavedAtUtc = summary.LastRecommendationSavedAtUtc,
            AiRecommendation = new AiRecommendationPanelViewModel
            {
                TargetDate = summary.TargetDate
            }
        });
    }

    [HttpPost("ai-recommendation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAiRecommendation(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _aiPrepRecommendationService.GenerateAsync(
                new AiPrepRecommendationRequest
                {
                    TargetDate = targetDate
                },
                cancellationToken);

            return PartialView(
                "_AiRecommendationPartial",
                new AiRecommendationPanelViewModel
                {
                    TargetDate = response.TargetDate,
                    RecommendationText = response.RecommendationText,
                    IsAiGenerated = response.IsAiGenerated
                });
        }
        catch
        {
            return PartialView(
                "_AiRecommendationPartial",
                new AiRecommendationPanelViewModel
                {
                    TargetDate = targetDate == default
                        ? DateOnly.FromDateTime(DateTime.Today)
                        : targetDate,
                    ErrorMessage = "The recommendation panel could not generate guidance right now. Try again after refreshing the dashboard."
                });
        }
    }
}
