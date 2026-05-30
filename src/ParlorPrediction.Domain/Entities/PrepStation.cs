namespace ParlorPrediction.Domain.Entities;

public sealed class PrepStation
{
    public const int NameMaxLength = 80;
    public const int CodeMaxLength = 40;
    public const int DescriptionMaxLength = 250;

    private readonly List<PrepItem> _items = [];

    private PrepStation()
    {
    }

    public PrepStation(Guid id, string name, string code, string? description = null, bool isActive = true)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ApplyDetails(name, code, description);
        IsActive = isActive;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<PrepItem> Items => _items.AsReadOnly();

    public void Update(string name, string code, string? description = null)
    {
        ApplyDetails(name, code, description);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ApplyDetails(string name, string code, string? description)
    {
        Name = NormalizeRequired(name, nameof(name));
        Code = NormalizeRequired(code, nameof(code)).ToUpperInvariant();
        Description = NormalizeOptional(description);
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
