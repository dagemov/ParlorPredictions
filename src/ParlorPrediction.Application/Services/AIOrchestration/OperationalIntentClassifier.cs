using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.AIOrchestration;

public sealed partial class OperationalIntentClassifier : IOperationalIntentClassifier
{
    public Task<OperationalIntent> ClassifyAsync(
        string sourceText,
        DateOnly referenceDate,
        DateOnly? targetWeekStartDate = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceText);

        var normalizedText = NormalizeForMatching(sourceText);

        if (LooksLikeWeeklyClosing(normalizedText))
        {
            var linesLeftover = ParseFirstInt(normalizedText, LinePattern());
            var readyBalls = linesLeftover.HasValue
                ? linesLeftover.Value * DoughRules.StandardBatchBalls
                : ParseFirstInt(normalizedText, ReadyBallsPattern());
            var noPendingLoad =
                normalizedText.Contains("no quedo carga pendiente", StringComparison.Ordinal) ||
                normalizedText.Contains("no quedo mixed pendiente", StringComparison.Ordinal) ||
                normalizedText.Contains("0 mixed", StringComparison.Ordinal) ||
                normalizedText.Contains("0 mixed loads", StringComparison.Ordinal);
            var mixedLoads = noPendingLoad
                ? 0
                : ParseFirstInt(normalizedText, MixedLoadsPattern());
            var sundayLoadBalledMonday =
                normalizedText.Contains("domingo", StringComparison.Ordinal) &&
                normalizedText.Contains("lunes", StringComparison.Ordinal) &&
                normalizedText.Contains("bole", StringComparison.Ordinal);
            var reason = sundayLoadBalledMonday
                ? "Recovered Sunday load balled Monday morning."
                : "Operational weekly closing narrative interpreted from manager notes.";
            var summary = $"Weekly closing intent with {readyBalls ?? 0} ready balls and {mixedLoads ?? 0} mixed loads.";

            return Task.FromResult<OperationalIntent>(new WeeklyClosingIntent(
                sourceText.Trim(),
                summary,
                referenceDate,
                NormalizeClosingWeekStart(targetWeekStartDate ?? referenceDate),
                linesLeftover,
                readyBalls,
                mixedLoads,
                noPendingLoad,
                sundayLoadBalledMonday,
                reason));
        }

        if (LooksLikeProduction(normalizedText))
        {
            var mentionsLoadCreated = normalizedText.Contains("carga", StringComparison.Ordinal) ||
                normalizedText.Contains("load", StringComparison.Ordinal);
            var mentionsBalling = normalizedText.Contains("bole", StringComparison.Ordinal) ||
                normalizedText.Contains("ball", StringComparison.Ordinal);
            var summary = "Production intent interpreted from operational narrative.";

            return Task.FromResult<OperationalIntent>(new ProductionIntent(
                sourceText.Trim(),
                summary,
                referenceDate,
                mentionsLoadCreated,
                mentionsBalling,
                ParseFirstInt(normalizedText, QuantityPattern()),
                sourceText.Trim()));
        }

        if (LooksLikeConsumption(normalizedText))
        {
            return Task.FromResult<OperationalIntent>(new ConsumptionIntent(
                sourceText.Trim(),
                "Consumption intent interpreted from operational narrative.",
                referenceDate,
                normalizedText.Contains("reball", StringComparison.Ordinal) ||
                normalizedText.Contains("rebole", StringComparison.Ordinal),
                ParseFirstInt(normalizedText, QuantityPattern()),
                sourceText.Trim()));
        }

        if (LooksLikeInventory(normalizedText))
        {
            return Task.FromResult<OperationalIntent>(new InventoryIntent(
                sourceText.Trim(),
                "Inventory intent interpreted from operational narrative.",
                referenceDate,
                ParseFirstInt(normalizedText, ReadyBallsPattern()),
                ParseFirstInt(normalizedText, MixedLoadsPattern()),
                sourceText.Trim()));
        }

        if (LooksLikeSales(normalizedText))
        {
            return Task.FromResult<OperationalIntent>(new SalesIntent(
                sourceText.Trim(),
                "Sales intent interpreted from operational narrative.",
                referenceDate,
                null,
                ParseFirstInt(normalizedText, QuantityPattern())));
        }

        return Task.FromResult<OperationalIntent>(new UnknownIntent(
            sourceText.Trim(),
            "The narrative did not match a supported operational intent with enough confidence.",
            referenceDate));
    }

    private static bool LooksLikeWeeklyClosing(string normalizedText)
    {
        return normalizedText.Contains("weekly closing", StringComparison.Ordinal) ||
            normalizedText.Contains("carryover", StringComparison.Ordinal) ||
            normalizedText.Contains("sobraron", StringComparison.Ordinal) ||
            normalizedText.Contains("lineas", StringComparison.Ordinal) ||
            normalizedText.Contains("carga pendiente", StringComparison.Ordinal) ||
            (normalizedText.Contains("domingo", StringComparison.Ordinal) &&
             normalizedText.Contains("lunes", StringComparison.Ordinal) &&
             normalizedText.Contains("bole", StringComparison.Ordinal));
    }

    private static bool LooksLikeProduction(string normalizedText)
    {
        return normalizedText.Contains("carga", StringComparison.Ordinal) ||
            normalizedText.Contains("load", StringComparison.Ordinal) ||
            normalizedText.Contains("bole", StringComparison.Ordinal) ||
            normalizedText.Contains("ball", StringComparison.Ordinal);
    }

    private static bool LooksLikeConsumption(string normalizedText)
    {
        return normalizedText.Contains("consumo", StringComparison.Ordinal) ||
            normalizedText.Contains("used", StringComparison.Ordinal) ||
            normalizedText.Contains("uso", StringComparison.Ordinal) ||
            normalizedText.Contains("reball", StringComparison.Ordinal);
    }

    private static bool LooksLikeInventory(string normalizedText)
    {
        return normalizedText.Contains("inventario", StringComparison.Ordinal) ||
            normalizedText.Contains("ready now", StringComparison.Ordinal) ||
            normalizedText.Contains("estado fisico", StringComparison.Ordinal) ||
            normalizedText.Contains("physically", StringComparison.Ordinal);
    }

    private static bool LooksLikeSales(string normalizedText)
    {
        return normalizedText.Contains("ventas", StringComparison.Ordinal) ||
            normalizedText.Contains("sales", StringComparison.Ordinal) ||
            normalizedText.Contains("rejas", StringComparison.Ordinal);
    }

    private static int? ParseFirstInt(string input, Regex pattern)
    {
        var match = pattern.Match(input);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups[1].Value, out var value)
            ? value
            : null;
    }

    private static string NormalizeForMatching(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private static DateOnly NormalizeClosingWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    [GeneratedRegex(@"(\d+)\s*lineas?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinePattern();

    [GeneratedRegex(@"(\d+)\s*(?:ready\s*balls|bolas?\s*listas?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReadyBallsPattern();

    [GeneratedRegex(@"(\d+)\s*(?:mixed\s*loads?|cargas?\s*pendientes?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MixedLoadsPattern();

    [GeneratedRegex(@"(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex QuantityPattern();
}
