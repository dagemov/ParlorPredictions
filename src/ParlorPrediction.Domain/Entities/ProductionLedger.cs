namespace ParlorPrediction.Domain.Entities;

public sealed class ProductionLedger
{
    public const int SourceTypeMaxLength = 80;
    public const int NotesMaxLength = 500;

    private ProductionLedger()
    {
    }

    public ProductionLedger(
        Guid id,
        DateOnly occurredOn,
        string sourceType,
        Guid sourceEntityId,
        int totalBallsCreated,
        int ballsCompleted,
        int ballsReballed,
        int ballsDiscarded,
        string? notes = null,
        DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        OccurredOn = occurredOn == default
            ? throw new ArgumentException("Occurred on date is required.", nameof(occurredOn))
            : occurredOn;
        SourceType = NormalizeRequired(sourceType, nameof(sourceType), SourceTypeMaxLength);
        SourceEntityId = sourceEntityId == Guid.Empty
            ? throw new ArgumentException("Source entity id is required.", nameof(sourceEntityId))
            : sourceEntityId;
        TotalBallsCreated = EnsureNonNegative(totalBallsCreated, nameof(totalBallsCreated));
        BallsCompleted = EnsureNonNegative(ballsCompleted, nameof(ballsCompleted));
        BallsReballed = EnsureNonNegative(ballsReballed, nameof(ballsReballed));
        BallsDiscarded = EnsureNonNegative(ballsDiscarded, nameof(ballsDiscarded));
        Notes = NormalizeOptional(notes, NotesMaxLength);
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public string SourceType { get; private set; } = string.Empty;

    public Guid SourceEntityId { get; private set; }

    public int TotalBallsCreated { get; private set; }

    public int BallsCompleted { get; private set; }

    public int BallsReballed { get; private set; }

    public int BallsDiscarded { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private static int EnsureNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
        }

        return value;
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
