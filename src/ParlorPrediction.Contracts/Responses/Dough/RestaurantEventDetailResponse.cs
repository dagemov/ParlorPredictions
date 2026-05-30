namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class RestaurantEventDetailResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public int EstimatedPizzas { get; set; }

    public int EstimatedDoughBalls { get; set; }

    public bool AllowShortFermentation { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }
}
