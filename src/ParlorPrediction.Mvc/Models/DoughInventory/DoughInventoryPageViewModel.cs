namespace ParlorPrediction.Mvc.Models.DoughInventory;

public sealed class DoughInventoryPageViewModel
{
    public DateOnly TargetDate { get; set; }

    public int HistoricalWeeksToUse { get; set; } = 8;

    public DoughInventorySummaryViewModel Summary { get; set; } = new();

    public IReadOnlyList<DoughInventorySourceCardViewModel> Sources { get; set; } =
        Array.Empty<DoughInventorySourceCardViewModel>();
}
