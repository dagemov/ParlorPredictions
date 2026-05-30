namespace ParlorPrediction.Mvc.Models.PrepData;

public sealed class RestaurantEventListPageViewModel
{
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public string? Term { get; init; }

    public bool ActiveOnly { get; init; } = true;

    public IReadOnlyList<RestaurantEventListItemViewModel> Events { get; init; } = Array.Empty<RestaurantEventListItemViewModel>();

    public string? StatusType { get; init; }

    public string? StatusMessage { get; init; }
}
