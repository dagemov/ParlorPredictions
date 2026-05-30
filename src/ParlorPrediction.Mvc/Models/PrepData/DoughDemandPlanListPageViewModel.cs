using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.PrepData;

public sealed class DoughDemandPlanListPageViewModel
{
    public DayOfWeek? DayOfWeek { get; init; }

    public string? SourceTerm { get; init; }

    public bool ActiveOnly { get; init; } = true;

    public IReadOnlyList<SelectListItem> DayOfWeekOptions { get; init; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<DoughDemandPlanListItemViewModel> DemandPlans { get; init; } = Array.Empty<DoughDemandPlanListItemViewModel>();

    public string? StatusType { get; init; }

    public string? StatusMessage { get; init; }
}
