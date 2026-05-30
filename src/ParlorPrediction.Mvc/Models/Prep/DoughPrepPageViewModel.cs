namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughPrepPageViewModel
{
    public DateOnly TargetDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public bool CanManageRecommendations { get; set; }

    public DoughRecommendationViewModel? Recommendation { get; set; }

    public IReadOnlyList<DoughTaskViewModel> Tasks { get; set; } = Array.Empty<DoughTaskViewModel>();
}
