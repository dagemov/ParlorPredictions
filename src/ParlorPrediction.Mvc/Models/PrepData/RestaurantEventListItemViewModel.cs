namespace ParlorPrediction.Mvc.Models.PrepData;

public sealed class RestaurantEventListItemViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateOnly EventDate { get; init; }

    public int EstimatedPizzas { get; init; }

    public int EstimatedDoughBalls { get; init; }

    public bool AllowShortFermentation { get; init; }

    public string? Notes { get; init; }

    public bool IsActive { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
