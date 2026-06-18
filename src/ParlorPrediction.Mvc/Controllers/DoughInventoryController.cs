using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.DoughInventory;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)},{nameof(ApplicationRole.PizzaMaker)}")]
[Route("prep/dough-inventory")]
public sealed class DoughInventoryController : Controller
{
    private const int DefaultHistoricalWeeksToUse = 8;

    private readonly IDoughInventoryImpactReadService _doughInventoryImpactReadService;

    public DoughInventoryController(IDoughInventoryImpactReadService doughInventoryImpactReadService)
    {
        _doughInventoryImpactReadService = doughInventoryImpactReadService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        DateOnly? targetDate,
        int historicalWeeksToUse = DefaultHistoricalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        var selectedDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
        var response = await _doughInventoryImpactReadService.GetInventoryImpactAsync(
            new GetDoughInventoryImpactRequest
            {
                ReferenceDate = selectedDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);

        return View(DoughInventoryViewModelMapper.MapPage(response, historicalWeeksToUse));
    }
}
