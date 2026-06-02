using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Domain.Entities;

public sealed class ManagerPrepRecommendation
{
    public const int RecommendationTextMaxLength = 1000;
    public const int ReasonMaxLength = 1000;

    private ManagerPrepRecommendation()
    {
    }

    private ManagerPrepRecommendation(
        Guid id,
        DateOnly recommendationDate,
        Guid prepItemId,
        string recommendationText,
        int recommendedBalls,
        string reason,
        string createdByUserId)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetRecommendationDate(recommendationDate);
        SetPrepItem(prepItemId);
        SetRecommendation(recommendationText, recommendedBalls, reason);
        CreatedByUserId = NormalizeRequired(createdByUserId, nameof(createdByUserId));
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public DateOnly RecommendationDate { get; private set; }

    public Guid PrepItemId { get; private set; }

    public string RecommendationText { get; private set; } = null!;

    public int RecommendedBalls { get; private set; }

    public int RecommendedCases { get; private set; }

    public int RecommendedLoads { get; private set; }

    public string Reason { get; private set; } = null!;

    public string CreatedByUserId { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public PrepItem PrepItem { get; private set; } = null!;

    public User CreatedByUser { get; private set; } = null!;

    public static ManagerPrepRecommendation Create(
        DateOnly recommendationDate,
        Guid prepItemId,
        string recommendationText,
        int recommendedBalls,
        string reason,
        string createdByUserId,
        Guid? id = null)
    {
        return new ManagerPrepRecommendation(
            id ?? Guid.Empty,
            recommendationDate,
            prepItemId,
            recommendationText,
            recommendedBalls,
            reason,
            createdByUserId);
    }

    private void SetRecommendationDate(DateOnly recommendationDate)
    {
        if (recommendationDate == default)
        {
            throw new ArgumentException("Recommendation date is required.", nameof(recommendationDate));
        }

        RecommendationDate = recommendationDate;
    }

    private void SetPrepItem(Guid prepItemId)
    {
        if (prepItemId == Guid.Empty)
        {
            throw new ArgumentException("Prep item id is required.", nameof(prepItemId));
        }

        PrepItemId = prepItemId;
    }

    private void SetRecommendation(string recommendationText, int recommendedBalls, string reason)
    {
        RecommendationText = NormalizeRequired(recommendationText, nameof(recommendationText));
        RecommendedBalls = EnsureNonNegative(recommendedBalls, nameof(recommendedBalls));
        RecommendedCases = DoughRules.ConvertBallsToCases(RecommendedBalls);
        RecommendedLoads = DoughRules.ConvertBallsToLoads(RecommendedBalls);
        Reason = NormalizeRequired(reason, nameof(reason));
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
