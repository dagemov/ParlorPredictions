namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityReviewFilterViewModel
{
    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? CreatedOrBalledFromDate { get; set; }

    public DateOnly? CreatedOrBalledToDate { get; set; }

    public DateOnly? ReballedFromDate { get; set; }

    public DateOnly? ReballedToDate { get; set; }

    public string? CurrentStatus { get; set; }
}
