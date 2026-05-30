namespace ParlorPrediction.Domain.Entities;

public sealed class PrepItem
{
    public const int NameMaxLength = 100;
    public const int CodeMaxLength = 50;
    public const int DescriptionMaxLength = 250;

    private PrepItem()
    {
    }

    public PrepItem(Guid id, Guid prepStationId, string name, string code, string? description = null, bool isActive = true)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetPrepStation(prepStationId);
        ApplyDetails(name, code, description);
        IsActive = isActive;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PrepStationId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public PrepStation PrepStation { get; private set; } = null!;

    public void Update(string name, string code, string? description = null)
    {
        ApplyDetails(name, code, description);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReassignToStation(Guid prepStationId)
    {
        SetPrepStation(prepStationId);
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

    private void SetPrepStation(Guid prepStationId)
    {
        if (prepStationId == Guid.Empty)
        {
            throw new ArgumentException("Prep station id is required.", nameof(prepStationId));
        }

        PrepStationId = prepStationId;
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
