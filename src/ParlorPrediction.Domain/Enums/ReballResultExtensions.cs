namespace ParlorPrediction.Domain.Enums;

public static class ReballResultExtensions
{
    public static string Normalize(string? value)
    {
        return (value?.Trim() ?? string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
    }

    public static bool TryParse(string? value, out ReballResult result)
    {
        var normalized = Normalize(value);

        return normalized.ToLowerInvariant() switch
        {
            "partialrecovered" => Return(ReballResult.PartialRecovered, out result),
            "discarded" => Return(ReballResult.Discarded, out result),
            "managercancelled" => Return(ReballResult.ManagerCancelled, out result),
            _ => Enum.TryParse(normalized, true, out result)
        };
    }

    private static bool Return(ReballResult value, out ReballResult result)
    {
        result = value;
        return true;
    }
}
