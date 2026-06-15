namespace ParlorPrediction.Mvc.Models.DoughUsage;

public sealed class DoughUsageEntryPageViewModel
{
    public DateOnly UsageDate { get; set; }

    public bool CanManageTraces { get; set; }

    public DoughUsageTraceFormViewModel Form { get; set; } = new();

    public IReadOnlyList<DoughUsageSourceCardViewModel> AvailableSources { get; set; } = Array.Empty<DoughUsageSourceCardViewModel>();

    public IReadOnlyList<DoughUsageTraceListItemViewModel> RecentTraces { get; set; } = Array.Empty<DoughUsageTraceListItemViewModel>();
}
