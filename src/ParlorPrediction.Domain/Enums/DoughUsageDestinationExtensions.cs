namespace ParlorPrediction.Domain.Enums;

public static class DoughUsageDestinationExtensions
{
    public static string Normalize(string? value)
    {
        return (value?.Trim() ?? string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty);
    }

    public static bool TryParse(string? value, out DoughUsageDestination destination)
    {
        var normalized = Normalize(value);

        return normalized.ToLowerInvariant() switch
        {
            "restaurant" => Return(DoughUsageDestination.Restaurant, out destination),
            "event" => Return(DoughUsageDestination.Event, out destination),
            "farmersmarket" => Return(DoughUsageDestination.FarmersMarket, out destination),
            _ => Enum.TryParse(normalized, true, out destination)
        };
    }

    private static bool Return(DoughUsageDestination value, out DoughUsageDestination destination)
    {
        destination = value;
        return true;
    }
}
