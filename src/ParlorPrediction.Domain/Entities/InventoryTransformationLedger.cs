namespace ParlorPrediction.Domain.Entities;

public sealed class InventoryTransformationLedger
{
    public const int SourceTypeMaxLength = 80;
    public const int NotesMaxLength = 500;

    private InventoryTransformationLedger()
    {
    }

    public InventoryTransformationLedger(
        Guid id,
        DateOnly occurredOn,
        string sourceType,
        Guid sourceEntityId,
        int ballsRecovered,
        int ballsDiscarded,
        int ballsReclassified,
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
        BallsRecovered = EnsureNonNegative(ballsRecovered, nameof(ballsRecovered));
        BallsDiscarded = EnsureNonNegative(ballsDiscarded, nameof(ballsDiscarded));
        BallsReclassified = EnsureNonNegative(ballsReclassified, nameof(ballsReclassified));
        Notes = NormalizeOptional(notes, NotesMaxLength);
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public string SourceType { get; private set; } = string.Empty;

    public Guid SourceEntityId { get; private set; }

    public int BallsRecovered { get; private set; }

    public int BallsDiscarded { get; private set; }

    public int BallsReclassified { get; private set; }

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
