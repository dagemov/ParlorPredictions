namespace ParlorPrediction.Domain.Enums;

public static class DoughLossReasonExtensions
{
    public static string Normalize(string? value)
    {
        return (value?.Trim() ?? string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
    }

    public static bool TryParse(string? value, out DoughLossReason reason)
    {
        var normalized = Normalize(value);

        return normalized.ToLowerInvariant() switch
        {
            "toohot" => Return(DoughLossReason.TooHot, out reason),
            "overfermented" => Return(DoughLossReason.OverFermented, out reason),
            "storedtoomanydays" => Return(DoughLossReason.StoredTooManyDays, out reason),
            "contamination" => Return(DoughLossReason.Contamination, out reason),
            "fifonotfollowed" => Return(DoughLossReason.FifoNotFollowed, out reason),
            "notsoldenough" => Return(DoughLossReason.NotSoldEnough, out reason),
            "overproduced" => Return(DoughLossReason.OverProduced, out reason),
            "managerdecision" => Return(DoughLossReason.ManagerDecision, out reason),
            "other" => Return(DoughLossReason.Other, out reason),
            _ => Enum.TryParse(normalized, true, out reason)
        };
    }

    private static bool Return(DoughLossReason value, out DoughLossReason reason)
    {
        reason = value;
        return true;
    }
}
