namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughKitchenAttentionItemViewModel
{
    public string Title { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public int QuantityBalls { get; set; }

    public string Detail { get; set; } = string.Empty;

    public string? SecondaryDetail { get; set; }

    public bool IsMustUsePriority { get; set; }
}
