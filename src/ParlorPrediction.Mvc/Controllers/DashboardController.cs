using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Contracts.Requests.Ai;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.Dashboard;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("dashboard")]
public sealed class DashboardController : Controller
{
    private readonly IAiPrepRecommendationService _aiPrepRecommendationService;

    public DashboardController(
        IAiPrepRecommendationService aiPrepRecommendationService)
    {
        _aiPrepRecommendationService = aiPrepRecommendationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        DateOnly? targetDate,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        return RedirectToAction("Index", "Home", new { targetDate = selectedDate.ToString("yyyy-MM-dd") });
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
