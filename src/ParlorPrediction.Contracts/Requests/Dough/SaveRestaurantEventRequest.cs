namespace ParlorPrediction.Contracts.Requests.Dough;

public sealed class SaveRestaurantEventRequest
{
    public DateOnly EventDate { get; init; }

    public string Name { get; init; } = string.Empty;

    public int EstimatedPizzas { get; init; }

    public int EstimatedDoughBalls { get; init; }

    public bool AllowShortFermentation { get; init; }

    public string? Notes { get; init; }

    public bool IsActive { get; init; } = true;
}
