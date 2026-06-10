namespace ParlorPrediction.Domain.Entities;

public sealed class DailyDoughClosing
{
    public const int NotesMaxLength = 1000;
    public const int CorrectionNoteMaxLength = 1000;

    private DailyDoughClosing()
    {
    }

    private DailyDoughClosing(
        Guid id,
        DateOnly closingDate,
        DateOnly weekStartDate,
        int forecastNeededBalls,
        int actualUsedBalls,
        string closedByUserId,
        DateTime closedAtUtc,
        string? notes)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetClosingDate(closingDate, weekStartDate);
        SetCounts(forecastNeededBalls, actualUsedBalls);
        ClosedByUserId = NormalizeRequired(closedByUserId, nameof(closedByUserId));
        ClosedAtUtc = NormalizeTimestamp(closedAtUtc, nameof(closedAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly ClosingDate { get; private set; }

    public DateOnly WeekStartDate { get; private set; }

    public int ForecastNeededBalls { get; private set; }

    public int ActualUsedBalls { get; private set; }

    public int DailyVariance => ForecastNeededBalls - ActualUsedBalls;

    public string? Notes { get; private set; }

    public string ClosedByUserId { get; private set; } = null!;

    public DateTime ClosedAtUtc { get; private set; }

    public string? CorrectedByUserId { get; private set; }

    public DateTime? CorrectedAtUtc { get; private set; }

    public string? CorrectionNote { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public User ClosedByUser { get; private set; } = null!;

    public User? CorrectedByUser { get; private set; }

    public bool WasCorrected => CorrectedAtUtc.HasValue;

    public static DailyDoughClosing Create(
        DateOnly closingDate,
        DateOnly weekStartDate,
        int forecastNeededBalls,
        int actualUsedBalls,
        string closedByUserId,
        DateTime closedAtUtc,
        string? notes = null,
        Guid? id = null)
    {
        return new DailyDoughClosing(
            id ?? Guid.Empty,
            closingDate,
            weekStartDate,
            forecastNeededBalls,
            actualUsedBalls,
            closedByUserId,
            closedAtUtc,
            notes);
    }

    public void Correct(
        int forecastNeededBalls,
        int actualUsedBalls,
        string correctedByUserId,
        DateTime correctedAtUtc,
        string? notes = null,
        string? correctionNote = null)
    {
        SetCounts(forecastNeededBalls, actualUsedBalls);
        CorrectedByUserId = NormalizeRequired(correctedByUserId, nameof(correctedByUserId));
        CorrectedAtUtc = NormalizeTimestamp(correctedAtUtc, nameof(correctedAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        CorrectionNote = NormalizeOptional(correctionNote, CorrectionNoteMaxLength, nameof(correctionNote));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetClosingDate(DateOnly closingDate, DateOnly weekStartDate)
    {
        if (closingDate == default)
        {
            throw new ArgumentException("Closing date is required.", nameof(closingDate));
        }

        if (weekStartDate == default)
        {
            throw new ArgumentException("Week start date is required.", nameof(weekStartDate));
        }

        if (closingDate < weekStartDate || closingDate > weekStartDate.AddDays(WeeklyDoughClosing.OperationalWeekLengthDays - 1))
        {
            throw new ArgumentException("Closing date must fall within the operational week (Tuesday through Sunday).", nameof(closingDate));
        }

        ClosingDate = closingDate;
        WeekStartDate = weekStartDate;
    }

    private void SetCounts(int forecastNeededBalls, int actualUsedBalls)
    {
        ForecastNeededBalls = EnsureNonNegative(forecastNeededBalls, nameof(forecastNeededBalls));
        ActualUsedBalls = EnsureNonNegative(actualUsedBalls, nameof(actualUsedBalls));
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

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static DateTime NormalizeTimestamp(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("Timestamp is required.", parameterName);
        }

        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}
