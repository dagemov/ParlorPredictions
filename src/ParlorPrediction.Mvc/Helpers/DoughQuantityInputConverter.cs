using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Helpers;

public static class DoughQuantityInputConverter
{
    public static bool TryConvertToBalls(
        string? unitValue,
        int quantityValue,
        out int quantityBalls,
        out string validationMessage)
    {
        if (quantityValue <= 0)
        {
            quantityBalls = 0;
            validationMessage = "Enter a quantity greater than zero.";
            return false;
        }

        if (!Enum.TryParse<DoughQuantityUnit>(unitValue, true, out var quantityUnit))
        {
            quantityBalls = 0;
            validationMessage = "Choose whether you are entering dough balls, cases, or full loads.";
            return false;
        }

        quantityBalls = DoughRules.ConvertToBalls(quantityValue, quantityUnit);
        validationMessage = string.Empty;
        return true;
    }

    public static string BuildCompletedPreviewText(string? unitValue, int quantityValue)
    {
        return TryConvertToBalls(unitValue, quantityValue, out var quantityBalls, out _)
            ? $"This will count as {quantityBalls} dough balls completed."
            : "Choose a completion type and quantity to preview the dough balls total.";
    }

    public static string BuildPlannedPreviewText(string? unitValue, int quantityValue)
    {
        return TryConvertToBalls(unitValue, quantityValue, out var quantityBalls, out _)
            ? $"This task will plan {quantityBalls} dough balls for the kitchen."
            : "Choose a quantity type and amount to preview the dough planned for this task.";
    }

    public static string BuildRecommendedPreviewText(string? unitValue, int quantityValue)
    {
        return TryConvertToBalls(unitValue, quantityValue, out var quantityBalls, out _)
            ? $"This manager note will recommend {quantityBalls} dough balls."
            : "Choose a quantity type and amount to preview the dough covered by this recommendation.";
    }
}
