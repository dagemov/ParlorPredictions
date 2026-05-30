namespace ParlorPrediction.Domain.Entities;

public sealed class RestaurantEvent
{
    public const int NameMaxLength = 120;
    public const int NotesMaxLength = 300;
    public const int ExternalCalendarEventIdMaxLength = 200;

    private RestaurantEvent()
    {
    }

    public RestaurantEvent(
        Guid id,
        DateOnly eventDate,
        string name,
        int estimatedPizzas,
        int estimatedDoughBalls,
        bool allowShortFermentation,
        string? notes = null,
        string? externalCalendarEventId = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetEventDate(eventDate);
        Name = NormalizeRequired(name, nameof(name));
        EstimatedPizzas = EnsureNonNegative(estimatedPizzas, nameof(estimatedPizzas));
        EstimatedDoughBalls = EnsureNonNegative(estimatedDoughBalls, nameof(estimatedDoughBalls));
        AllowShortFermentation = allowShortFermentation;
        Notes = NormalizeOptional(notes);
        ExternalCalendarEventId = NormalizeOptional(externalCalendarEventId);
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public DateOnly EventDate { get; private set; }

    public int EstimatedPizzas { get; private set; }

    public int EstimatedDoughBalls { get; private set; }

    public bool AllowShortFermentation { get; private set; }

    public string? ExternalCalendarEventId { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateEvent(
        DateOnly eventDate,
        string name,
        int estimatedPizzas,
        int estimatedDoughBalls,
        bool allowShortFermentation,
        string? notes = null,
        string? externalCalendarEventId = null)
    {
        SetEventDate(eventDate);
        Name = NormalizeRequired(name, nameof(name));
        EstimatedPizzas = EnsureNonNegative(estimatedPizzas, nameof(estimatedPizzas));
        EstimatedDoughBalls = EnsureNonNegative(estimatedDoughBalls, nameof(estimatedDoughBalls));
        AllowShortFermentation = allowShortFermentation;
        Notes = NormalizeOptional(notes);
        ExternalCalendarEventId = NormalizeOptional(externalCalendarEventId);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void LinkExternalCalendarEvent(string? externalCalendarEventId)
    {
        ExternalCalendarEventId = NormalizeOptional(externalCalendarEventId);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetEventDate(DateOnly eventDate)
    {
        if (eventDate == default)
        {
            throw new ArgumentException("Event date is required.", nameof(eventDate));
        }

        EventDate = eventDate;
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
