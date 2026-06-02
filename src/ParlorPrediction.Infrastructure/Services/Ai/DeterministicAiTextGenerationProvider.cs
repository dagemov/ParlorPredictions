using System.Globalization;
using ParlorPrediction.Application.Interfaces.Ai;

namespace ParlorPrediction.Infrastructure.Services.Ai;

public sealed class DeterministicAiTextGenerationProvider : IAiTextGenerationProvider
{
    public Task<string> GenerateTextAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var values = ParsePrompt(prompt);

        var targetDate = GetDate(values, "Date");
        var hasRecommendation = GetBool(values, "HasRecommendation");
        var requiredBalls = GetInt(values, "RequiredBalls");
        var availableBalls = GetInt(values, "AvailableBalls");
        var missingBalls = GetInt(values, "MissingBalls");
        var recommendedCases = GetInt(values, "RecommendedCases");
        var recommendedLoads = GetInt(values, "RecommendedLoads");
        var pendingTasks = GetInt(values, "PendingTasks");
        var completedTasks = GetInt(values, "CompletedTasks");
        var reason = GetString(values, "LastRecommendationReason");
        var managerRecommendationDate = GetString(values, "ManagerRecommendationDate");
        var managerRecommendationBalls = GetInt(values, "ManagerRecommendationBalls");
        var managerRecommendationText = GetString(values, "ManagerRecommendationText");
        var managerRecommendationReason = GetString(values, "ManagerRecommendationReason");
        var managerSentence =
            string.IsNullOrWhiteSpace(managerRecommendationText) ||
            string.Equals(managerRecommendationText, "None", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : $" A recent manager note for this prep window points to about {managerRecommendationBalls} dough balls: {managerRecommendationText} {managerRecommendationReason}".TrimEnd();

        if (!hasRecommendation)
        {
            return Task.FromResult(
                $"No saved dough recommendation exists for {targetDate:MMMM d, yyyy}. The manager should generate or save a Dough Prep recommendation first so shortage, cases, and task status can be reviewed from one snapshot before taking action.{managerSentence}".Trim());
        }

        var shortageSentence = missingBalls > 0
            ? $"For {targetDate:MMMM d, yyyy}, dough supply is short by {missingBalls} ball{Pluralize(missingBalls)}."
            : $"For {targetDate:MMMM d, yyyy}, current dough supply covers the expected demand.";

        var productionSentence = missingBalls > 0
            ? $"The saved recommendation points to {recommendedCases} case{Pluralize(recommendedCases)} across {recommendedLoads} load{Pluralize(recommendedLoads)}, with {availableBalls} available ball{Pluralize(availableBalls)} against {requiredBalls} required."
            : $"No extra dough production load is recommended right now, because {availableBalls} available ball{Pluralize(availableBalls)} cover the {requiredBalls} required.";

        string taskSentence;
        if (pendingTasks > 0)
        {
            taskSentence = $"There {(pendingTasks == 1 ? "is" : "are")} {pendingTasks} pending dough task{Pluralize(pendingTasks)}, so the manager should confirm follow-through before treating the shortage as covered.";
        }
        else if (completedTasks > 0)
        {
            taskSentence = $"There {(completedTasks == 1 ? "is" : "are")} already {completedTasks} completed dough task{Pluralize(completedTasks)}, so no immediate extra action is needed unless sales, events, or inventory change.";
        }
        else
        {
            taskSentence = "No dough task has been completed yet, so the manager should decide whether the saved recommendation needs to become an operational task.";
        }

        var reasonSentence = string.IsNullOrWhiteSpace(reason) || string.Equals(reason, "None", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" Operational basis: {reason}";

        managerSentence =
            string.IsNullOrWhiteSpace(managerRecommendationText) ||
            string.Equals(managerRecommendationText, "None", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : $" The latest manager note for this prep window was saved on {managerRecommendationDate} and points to about {managerRecommendationBalls} dough balls: {managerRecommendationText} {managerRecommendationReason}".TrimEnd();

        return Task.FromResult($"{shortageSentence} {productionSentence} {taskSentence}{reasonSentence}{managerSentence}".Trim());
    }

    private static Dictionary<string, string> ParsePrompt(string prompt)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = prompt.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            values[key] = value;
        }

        return values;
    }

    private static DateOnly GetDate(IReadOnlyDictionary<string, string> values, string key)
    {
        return DateOnly.TryParseExact(
            GetString(values, key),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var value)
            ? value
            : DateOnly.FromDateTime(DateTime.Today);
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> values, string key)
    {
        return bool.TryParse(GetString(values, key), out var value) && value;
    }

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key)
    {
        return int.TryParse(GetString(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static string GetString(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static string Pluralize(int value)
    {
        return value == 1 ? string.Empty : "s";
    }
}
