namespace ParlorPrediction.Domain.Enums;

public static class DoughQualityStatusExtensions
{
    public static string Normalize(string? value)
    {
        return (value?.Trim() ?? string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
    }

    public static bool TryParse(string? value, out DoughQualityStatus status)
    {
        var normalized = Normalize(value);

        return normalized.ToLowerInvariant() switch
        {
            "good" => Return(DoughQualityStatus.Good, out status),
            "attention" => Return(DoughQualityStatus.Attention, out status),
            "reballed" => Return(DoughQualityStatus.Reballed, out status),
            "mustusenextday" => Return(DoughQualityStatus.MustUseNextDay, out status),
            "discarded" => Return(DoughQualityStatus.Discarded, out status),
            _ => Enum.TryParse(normalized, true, out status)
        };
    }

    private static bool Return(DoughQualityStatus value, out DoughQualityStatus status)
    {
        status = value;
        return true;
    }
}
