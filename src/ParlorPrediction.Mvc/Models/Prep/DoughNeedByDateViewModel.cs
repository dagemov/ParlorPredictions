namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughNeedByDateViewModel
{
    public DateOnly NeedDate { get; set; }

    public int RestaurantBaselineBalls { get; set; }

    public int EventBalls { get; set; }

    public int TotalRequiredBalls { get; set; }

    public DateOnly ProductionWindowStart { get; set; }

    public DateOnly ProductionWindowEnd { get; set; }

    public DateOnly RecommendedMakeDate { get; set; }

    public bool UsesShortFermentation { get; set; }

    public bool IsRecommendedForSelectedProductionDate { get; set; }
}
