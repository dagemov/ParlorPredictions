using System.Text.RegularExpressions;

namespace ParlorPrediction.Mvc.Models.DoughUsage;

public static partial class DoughUsageDisplayText
{
    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.Compiled)]
    private static partial Regex CapitalLetterBoundaryRegex();

    public static string Format(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return CapitalLetterBoundaryRegex()
            .Replace(value.Trim(), " $1")
            .Trim();
    }
}
