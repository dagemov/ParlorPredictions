using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Mvc.Helpers;
using ParlorPrediction.Mvc.Models.Prep;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("prep/recommendations")]
public sealed class PrepRecommendationsController : Controller
{
    private const string StatusTypeKey = "PrepRecommendationStatusType";
    private const string StatusMessageKey = "PrepRecommendationStatusMessage";

    private readonly IManagerPrepRecommendationService _managerPrepRecommendationService;
    private readonly IPrepCatalogReadService _prepCatalogReadService;

    public PrepRecommendationsController(
        IManagerPrepRecommendationService managerPrepRecommendationService,
        IPrepCatalogReadService prepCatalogReadService)
    {
        _managerPrepRecommendationService = managerPrepRecommendationService;
        _prepCatalogReadService = prepCatalogReadService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Create));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        DateOnly? recommendationDate,
        CancellationToken cancellationToken = default)
    {
        var catalogOptions = await _prepCatalogReadService.GetActiveOptionsAsync(cancellationToken);
        var defaultPrepItem = catalogOptions.PrepItems.FirstOrDefault();

        var model = await BuildPageViewModelAsync(
            new ManagerPrepRecommendationFormViewModel
            {
                RecommendationDate = recommendationDate ?? DateOnly.FromDateTime(DateTime.Today),
                PrepItemId = defaultPrepItem?.Id ?? Guid.Empty,
                QuantityUnit = nameof(Domain.Enums.DoughQuantityUnit.Balls)
            },
            cancellationToken);

        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ManagerPrepRecommendationFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveQuantityBalls(model.QuantityUnit, model.QuantityValue, out var quantityBalls, out var validationMessage))
        {
            ModelState.AddModelError(nameof(model.QuantityValue), validationMessage);
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildPageViewModelAsync(model, cancellationToken));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        try
        {
            await _managerPrepRecommendationService.CreateAsync(
                new SaveManagerPrepRecommendationRequest
                {
                    RecommendationDate = model.RecommendationDate,
                    PrepItemId = model.PrepItemId,
                    RecommendationText = model.RecommendationText,
                    RecommendedBalls = quantityBalls,
                    Reason = model.Reason,
                    CreatedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Manager recommendation saved.");
            return RedirectToAction(nameof(Create), new { recommendationDate = model.RecommendationDate.ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (IsFriendlyRecommendationException(exception))
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await BuildPageViewModelAsync(model, cancellationToken));
        }
    }

    private async Task<ManagerPrepRecommendationPageViewModel> BuildPageViewModelAsync(
        ManagerPrepRecommendationFormViewModel form,
        CancellationToken cancellationToken)
    {
        var catalogOptions = await _prepCatalogReadService.GetActiveOptionsAsync(cancellationToken);
        var recommendations = await _managerPrepRecommendationService.SearchAsync(
            fromDate: null,
            toDate: null,
            prepItemId: null,
            take: 12,
            cancellationToken: cancellationToken);

        return new ManagerPrepRecommendationPageViewModel
        {
            Form = form,
            PrepItemOptions = catalogOptions.PrepItems
                .Select(item => new SelectListItem(
                    item.Name,
                    item.Id.ToString(),
                    item.Id == form.PrepItemId))
                .ToArray(),
            RecentRecommendations = recommendations
                .Select(Map)
                .ToArray()
        };
    }

    private static ManagerPrepRecommendationListItemViewModel Map(ManagerPrepRecommendationListItemResponse recommendation)
    {
        return new ManagerPrepRecommendationListItemViewModel
        {
            Id = recommendation.Id,
            RecommendationDate = recommendation.RecommendationDate,
            PrepItemId = recommendation.PrepItemId,
            PrepItemName = recommendation.PrepItemName,
            RecommendationText = recommendation.RecommendationText,
            RecommendedBalls = recommendation.RecommendedBalls,
            RecommendedCases = recommendation.RecommendedCases,
            RecommendedLoads = recommendation.RecommendedLoads,
            Reason = recommendation.Reason,
            CreatedByUserName = recommendation.CreatedByUserName,
            CreatedAtUtc = recommendation.CreatedAtUtc
        };
    }

    private static bool TryResolveQuantityBalls(
        string? quantityUnit,
        int quantityValue,
        out int quantityBalls,
        out string validationMessage)
    {
        return DoughQuantityInputConverter.TryConvertToBalls(
            quantityUnit,
            quantityValue,
            out quantityBalls,
            out validationMessage);
    }

    private static bool IsFriendlyRecommendationException(Exception exception)
    {
        return exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException or
            KeyNotFoundException;
    }

    private void SetStatusMessage(string statusType, string message)
    {
        TempData[StatusTypeKey] = statusType;
        TempData[StatusMessageKey] = message;
    }
}
