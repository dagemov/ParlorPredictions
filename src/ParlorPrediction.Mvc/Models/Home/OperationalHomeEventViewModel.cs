namespace ParlorPrediction.Mvc.Models.Home;

public sealed class OperationalHomeEventViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public int EstimatedDoughBalls { get; set; }

    public bool AllowShortFermentation { get; set; }

    public string? Notes { get; set; }
}
