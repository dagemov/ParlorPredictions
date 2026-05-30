namespace ParlorPrediction.Domain.Entities;

public sealed class DoughInventorySnapshot
{
    public const int NotesMaxLength = 500;

    private DoughInventorySnapshot()
    {
    }

    public DoughInventorySnapshot(
        Guid id,
        DateOnly snapshotDate,
        int availableBalls,
        int newBalls,
        int oldBalls,
        int reservedBalls,
        int usedBalls,
        int wasteBalls,
        string? notes = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetSnapshotDate(snapshotDate);
        SetCounts(availableBalls, newBalls, oldBalls, reservedBalls, usedBalls, wasteBalls);
        Notes = NormalizeOptional(notes);
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly SnapshotDate { get; private set; }

    public int AvailableBalls { get; private set; }

    public int NewBalls { get; private set; }

    public int OldBalls { get; private set; }

    public int ReservedBalls { get; private set; }

    public int UsedBalls { get; private set; }

    public int WasteBalls { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateSnapshot(
        DateOnly snapshotDate,
        int availableBalls,
        int newBalls,
        int oldBalls,
        int reservedBalls,
        int usedBalls,
        int wasteBalls,
        string? notes = null)
    {
        SetSnapshotDate(snapshotDate);
        SetCounts(availableBalls, newBalls, oldBalls, reservedBalls, usedBalls, wasteBalls);
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetSnapshotDate(DateOnly snapshotDate)
    {
        if (snapshotDate == default)
        {
            throw new ArgumentException("Snapshot date is required.", nameof(snapshotDate));
        }

        SnapshotDate = snapshotDate;
    }

    private void SetCounts(
        int availableBalls,
        int newBalls,
        int oldBalls,
        int reservedBalls,
        int usedBalls,
        int wasteBalls)
    {
        AvailableBalls = EnsureNonNegative(availableBalls, nameof(availableBalls));
        NewBalls = EnsureNonNegative(newBalls, nameof(newBalls));
        OldBalls = EnsureNonNegative(oldBalls, nameof(oldBalls));
        ReservedBalls = EnsureNonNegative(reservedBalls, nameof(reservedBalls));
        UsedBalls = EnsureNonNegative(usedBalls, nameof(usedBalls));
        WasteBalls = EnsureNonNegative(wasteBalls, nameof(wasteBalls));
    }

    private static int EnsureNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
        }

        return value;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
