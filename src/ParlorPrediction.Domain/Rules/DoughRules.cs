using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Domain.Rules;

public static class DoughRules
{
    public const int ShortFermentationMinimumDays = 1;
    public const int ShortFermentationMaximumDays = 2;
    public const int BallsPerCase = 12;
    public const int StandardBatchCases = 14;
    public const int StandardBatchBalls = BallsPerCase * StandardBatchCases;
    public const int NormalFermentationMinimumDays = 2;
    public const int NormalFermentationMaximumDays = 4;
    public const int DefaultHistoricalWeeksToUse = 8;

    public static bool IsSummerEventMonth(int month)
    {
        return month is 6 or 7 or 8;
    }

    public static int ConvertToBalls(int quantity, DoughQuantityUnit unit)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        }

        return unit switch
        {
            DoughQuantityUnit.Balls => quantity,
            DoughQuantityUnit.Cases => quantity * BallsPerCase,
            DoughQuantityUnit.FullLoads => quantity * StandardBatchBalls,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), "The dough quantity unit is not supported.")
        };
    }

    public static int ConvertBallsToCases(int balls)
    {
        if (balls < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balls), "Dough balls cannot be negative.");
        }

        return balls == 0
            ? 0
            : (int)Math.Ceiling(balls / (double)BallsPerCase);
    }

    public static int ConvertBallsToLoads(int balls)
    {
        if (balls < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balls), "Dough balls cannot be negative.");
        }

        return balls == 0
            ? 0
            : (int)Math.Ceiling(balls / (double)StandardBatchBalls);
    }

    public static (DateOnly WindowStart, DateOnly WindowEnd, DateOnly RecommendedMakeDate) GetProductionWindow(
        DateOnly needDate,
        bool usesShortFermentation)
    {
        return usesShortFermentation
            ? (
                needDate.AddDays(-ShortFermentationMaximumDays),
                needDate.AddDays(-ShortFermentationMinimumDays),
                needDate.AddDays(-ShortFermentationMinimumDays))
            : (
                needDate.AddDays(-NormalFermentationMaximumDays),
                needDate.AddDays(-NormalFermentationMinimumDays),
                needDate.AddDays(-NormalFermentationMinimumDays));
    }
}
