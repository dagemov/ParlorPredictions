namespace ParlorPrediction.Domain.Entities;

public sealed class DoughPrepRecommendation
{
    public const int ReasonMaxLength = 1000;

    private DoughPrepRecommendation()
    {
    }

    private DoughPrepRecommendation(
        Guid id,
        DateOnly recommendationDate,
        int requiredBalls,
        int historicalAverageBalls,
        int eventEstimatedBalls,
        int availableBalls,
        int missingBalls,
        int recommendedCases,
        int recommendedLoads,
        bool shouldMakeDough,
        bool shouldBallDough,
        bool usesShortFermentationException,
        string reason)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetRecommendationDate(recommendationDate);
        SetQuantities(
            requiredBalls,
            historicalAverageBalls,
            eventEstimatedBalls,
            availableBalls,
            missingBalls,
            recommendedCases,
            recommendedLoads);
        ShouldMakeDough = shouldMakeDough;
        ShouldBallDough = shouldBallDough;
        UsesShortFermentationException = usesShortFermentationException;
        Reason = NormalizeRequired(reason, nameof(reason));
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly RecommendationDate { get; private set; }

    public int RequiredBalls { get; private set; }

    public int HistoricalAverageBalls { get; private set; }

    public int EventEstimatedBalls { get; private set; }

    public int AvailableBalls { get; private set; }

    public int MissingBalls { get; private set; }

    public int RecommendedCases { get; private set; }

    public int RecommendedLoads { get; private set; }

    public bool ShouldMakeDough { get; private set; }

    public bool ShouldBallDough { get; private set; }

    public bool UsesShortFermentationException { get; private set; }

    public string Reason { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static DoughPrepRecommendation FromCalculationSnapshot(
        DateOnly recommendationDate,
        int requiredBalls,
        int historicalAverageBalls,
        int eventEstimatedBalls,
        int availableBalls,
        int missingBalls,
        int recommendedCases,
        int recommendedLoads,
        bool shouldMakeDough,
        bool shouldBallDough,
        bool usesShortFermentationException,
        string reason,
        Guid? id = null)
    {
        return new DoughPrepRecommendation(
            id ?? Guid.Empty,
            recommendationDate,
            requiredBalls,
            historicalAverageBalls,
            eventEstimatedBalls,
            availableBalls,
            missingBalls,
            recommendedCases,
            recommendedLoads,
            shouldMakeDough,
            shouldBallDough,
            usesShortFermentationException,
            reason);
    }

    private void SetRecommendationDate(DateOnly recommendationDate)
    {
        if (recommendationDate == default)
        {
            throw new ArgumentException("Recommendation date is required.", nameof(recommendationDate));
        }

        RecommendationDate = recommendationDate;
    }

    private void SetQuantities(
        int requiredBalls,
        int historicalAverageBalls,
        int eventEstimatedBalls,
        int availableBalls,
        int missingBalls,
        int recommendedCases,
        int recommendedLoads)
    {
        RequiredBalls = EnsureNonNegative(requiredBalls, nameof(requiredBalls));
        HistoricalAverageBalls = EnsureNonNegative(historicalAverageBalls, nameof(historicalAverageBalls));
        EventEstimatedBalls = EnsureNonNegative(eventEstimatedBalls, nameof(eventEstimatedBalls));
        AvailableBalls = EnsureNonNegative(availableBalls, nameof(availableBalls));
        MissingBalls = EnsureNonNegative(missingBalls, nameof(missingBalls));
        RecommendedCases = EnsureNonNegative(recommendedCases, nameof(recommendedCases));
        RecommendedLoads = EnsureNonNegative(recommendedLoads, nameof(recommendedLoads));
    }

    private static int EnsureNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
        }

        return value;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }
}
