namespace ParlorPrediction.Domain.Entities;

public sealed class WeeklyDoughClosing
{
    public const int NotesMaxLength = 1000;
    public const int CorrectionNoteMaxLength = 1000;
    public const int OperationalWeekLengthDays = 6;

    private WeeklyDoughClosing()
    {
    }

    private WeeklyDoughClosing(
        Guid id,
        DateOnly weekStartDate,
        int neededBalls,
        int producedBalls,
        int usedBalls,
        int lostBalls,
        int leftoverReadyBalls,
        int leftoverAttentionBalls,
        int leftoverMixedLoads,
        string closedByUserId,
        DateTime closedAtUtc,
        string? notes)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetWeekWindow(weekStartDate, weekStartDate.AddDays(OperationalWeekLengthDays - 1));
        SetCounts(
            neededBalls,
            producedBalls,
            usedBalls,
            lostBalls,
            leftoverReadyBalls,
            leftoverAttentionBalls,
            leftoverMixedLoads);
        ClosedByUserId = NormalizeRequired(closedByUserId, nameof(closedByUserId));
        ClosedAtUtc = NormalizeTimestamp(closedAtUtc, nameof(closedAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly WeekStartDate { get; private set; }

    public DateOnly WeekEndDate { get; private set; }

    public int NeededBalls { get; private set; }

    public int ProducedBalls { get; private set; }

    public int UsedBalls { get; private set; }

    public int LostBalls { get; private set; }

    public int LeftoverReadyBalls { get; private set; }

    public int LeftoverAttentionBalls { get; private set; }

    public int LeftoverMixedLoads { get; private set; }

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

    public int CarryoverAvailableBalls => LeftoverReadyBalls + LeftoverAttentionBalls;

    public bool WasCorrected => CorrectedAtUtc.HasValue;

    public static WeeklyDoughClosing Create(
        DateOnly weekStartDate,
        int neededBalls,
        int producedBalls,
        int usedBalls,
        int lostBalls,
        int leftoverReadyBalls,
        int leftoverAttentionBalls,
        int leftoverMixedLoads,
        string closedByUserId,
        DateTime closedAtUtc,
        string? notes = null,
        Guid? id = null)
    {
        return new WeeklyDoughClosing(
            id ?? Guid.Empty,
            weekStartDate,
            neededBalls,
            producedBalls,
            usedBalls,
            lostBalls,
            leftoverReadyBalls,
            leftoverAttentionBalls,
            leftoverMixedLoads,
            closedByUserId,
            closedAtUtc,
            notes);
    }

    public void Correct(
        int neededBalls,
        int producedBalls,
        int usedBalls,
        int lostBalls,
        int leftoverReadyBalls,
        int leftoverAttentionBalls,
        int leftoverMixedLoads,
        string correctedByUserId,
        DateTime correctedAtUtc,
        string? notes = null,
        string? correctionNote = null)
    {
        SetCounts(
            neededBalls,
            producedBalls,
            usedBalls,
            lostBalls,
            leftoverReadyBalls,
            leftoverAttentionBalls,
            leftoverMixedLoads);
        CorrectedByUserId = NormalizeRequired(correctedByUserId, nameof(correctedByUserId));
        CorrectedAtUtc = NormalizeTimestamp(correctedAtUtc, nameof(correctedAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        CorrectionNote = NormalizeOptional(correctionNote, CorrectionNoteMaxLength, nameof(correctionNote));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetWeekWindow(DateOnly weekStartDate, DateOnly weekEndDate)
    {
        if (weekStartDate == default)
        {
            throw new ArgumentException("Week start date is required.", nameof(weekStartDate));
        }

        if (weekEndDate == default)
        {
            throw new ArgumentException("Week end date is required.", nameof(weekEndDate));
        }

        if (weekEndDate < weekStartDate)
        {
            throw new ArgumentException("Week end date must be on or after week start date.", nameof(weekEndDate));
        }

        if (weekEndDate.DayNumber - weekStartDate.DayNumber != OperationalWeekLengthDays - 1)
        {
            throw new ArgumentException("Weekly dough closing must cover the operational six-day window.", nameof(weekEndDate));
        }

        WeekStartDate = weekStartDate;
        WeekEndDate = weekEndDate;
    }

    private void SetCounts(
        int neededBalls,
        int producedBalls,
        int usedBalls,
        int lostBalls,
        int leftoverReadyBalls,
        int leftoverAttentionBalls,
        int leftoverMixedLoads)
    {
        NeededBalls = EnsureNonNegative(neededBalls, nameof(neededBalls));
        ProducedBalls = EnsureNonNegative(producedBalls, nameof(producedBalls));
        UsedBalls = EnsureNonNegative(usedBalls, nameof(usedBalls));
        LostBalls = EnsureNonNegative(lostBalls, nameof(lostBalls));
        LeftoverReadyBalls = EnsureNonNegative(leftoverReadyBalls, nameof(leftoverReadyBalls));
        LeftoverAttentionBalls = EnsureNonNegative(leftoverAttentionBalls, nameof(leftoverAttentionBalls));
        LeftoverMixedLoads = EnsureNonNegative(leftoverMixedLoads, nameof(leftoverMixedLoads));
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
