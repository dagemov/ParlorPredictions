using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityReviewPageViewModel
{
    public DoughQualityReviewFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<DoughQualityReviewCandidateViewModel> AttentionCandidates { get; set; } = Array.Empty<DoughQualityReviewCandidateViewModel>();

    public IReadOnlyList<DoughQualityReviewRecordViewModel> Records { get; set; } = Array.Empty<DoughQualityReviewRecordViewModel>();

    public bool CanCorrectStatus { get; set; }
}
