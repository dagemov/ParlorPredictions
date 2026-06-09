namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughLossAnalyticsFilterViewModel
{
    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? LossReason { get; set; }
}
