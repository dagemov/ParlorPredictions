namespace ParlorPrediction.Domain.Entities;

public sealed class DoughDemandPlan
{
    public const int SourceNameMaxLength = 120;
    public const int NotesMaxLength = 300;

    private DoughDemandPlan()
    {
    }

    public DoughDemandPlan(
        Guid id,
        DayOfWeek dayOfWeek,
        string sourceName,
        int minDoughBalls,
        int maxDoughBalls,
        string? notes = null,
        bool isActive = true)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        DayOfWeek = EnsureValidDayOfWeek(dayOfWeek);
        SourceName = NormalizeRequired(sourceName, nameof(sourceName));
        SetDemandRange(minDoughBalls, maxDoughBalls);
        Notes = NormalizeOptional(notes);
        IsActive = isActive;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public string SourceName { get; private set; } = null!;

    public int MinDoughBalls { get; private set; }

    public int MaxDoughBalls { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public int GetBaselineDoughBalls()
    {
        return (int)Math.Ceiling((MinDoughBalls + MaxDoughBalls) / 2d);
    }

    public void UpdatePlan(
        DayOfWeek dayOfWeek,
        string sourceName,
        int minDoughBalls,
        int maxDoughBalls,
        string? notes = null,
        bool isActive = true)
    {
        DayOfWeek = EnsureValidDayOfWeek(dayOfWeek);
        SourceName = NormalizeRequired(sourceName, nameof(sourceName));
        SetDemandRange(minDoughBalls, maxDoughBalls);
        Notes = NormalizeOptional(notes);
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetDemandRange(int minDoughBalls, int maxDoughBalls)
    {
        MinDoughBalls = EnsureNonNegative(minDoughBalls, nameof(minDoughBalls));
        MaxDoughBalls = EnsureNonNegative(maxDoughBalls, nameof(maxDoughBalls));

        if (MaxDoughBalls < MinDoughBalls)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDoughBalls), "Maximum dough balls cannot be lower than minimum dough balls.");
        }
    }

    private static DayOfWeek EnsureValidDayOfWeek(DayOfWeek dayOfWeek)
    {
        if (!Enum.IsDefined(dayOfWeek))
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Day of week is invalid.");
        }

        return dayOfWeek;
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

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
