namespace ParlorPrediction.Mvc.Models.DoughUsage;

public sealed class DoughReballPlanningPageViewModel
{
    public DateOnly ReferenceDate { get; set; }

    public IReadOnlyList<DoughUsageSourceCardViewModel> MustUseFirstSources { get; set; } = Array.Empty<DoughUsageSourceCardViewModel>();

    public IReadOnlyList<DoughUsageSourceCardViewModel> ReviewSources { get; set; } = Array.Empty<DoughUsageSourceCardViewModel>();

    public IReadOnlyList<DoughUsageSourceCardViewModel> ReballCandidates { get; set; } = Array.Empty<DoughUsageSourceCardViewModel>();

    public IReadOnlyList<DoughUsageSourceCardViewModel> DiscardCandidates { get; set; } = Array.Empty<DoughUsageSourceCardViewModel>();
}
