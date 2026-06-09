using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityCorrectStatusPageViewModel
{
    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DoughQualitySummaryViewModel Summary { get; set; } = new();

    public DoughQualityCorrectStatusFormViewModel Form { get; set; } = new();

    public DoughQualityReviewRecordViewModel? SelectedRecord { get; set; }

    public IReadOnlyList<DoughQualityReviewRecordViewModel> Records { get; set; } = Array.Empty<DoughQualityReviewRecordViewModel>();

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> LossReasonOptions { get; set; } = Array.Empty<SelectListItem>();
}
