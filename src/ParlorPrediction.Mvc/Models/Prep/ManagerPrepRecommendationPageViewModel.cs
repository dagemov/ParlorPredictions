using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class ManagerPrepRecommendationPageViewModel
{
    public ManagerPrepRecommendationFormViewModel Form { get; set; } = new();

    public IReadOnlyList<ManagerPrepRecommendationListItemViewModel> RecentRecommendations { get; set; } = Array.Empty<ManagerPrepRecommendationListItemViewModel>();

    public IReadOnlyList<SelectListItem> PrepItemOptions { get; set; } = Array.Empty<SelectListItem>();
}
