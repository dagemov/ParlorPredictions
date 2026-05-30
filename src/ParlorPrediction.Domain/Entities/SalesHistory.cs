namespace ParlorPrediction.Domain.Entities;

public sealed class SalesHistory
{
    public const int ProductNameMaxLength = 120;
    public const int NotesMaxLength = 300;

    private SalesHistory()
    {
    }

    public SalesHistory(
        Guid id,
        DateOnly saleDate,
        string productName,
        int quantitySold,
        int doughBallsUsed,
        string? notes = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetSaleDate(saleDate);
        ProductName = NormalizeRequired(productName, nameof(productName));
        QuantitySold = EnsureNonNegative(quantitySold, nameof(quantitySold));
        DoughBallsUsed = EnsureNonNegative(doughBallsUsed, nameof(doughBallsUsed));
        Notes = NormalizeOptional(notes);
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly SaleDate { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public string ProductName { get; private set; } = null!;

    public int QuantitySold { get; private set; }

    public int DoughBallsUsed { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateSale(
        DateOnly saleDate,
        string productName,
        int quantitySold,
        int doughBallsUsed,
        string? notes = null)
    {
        SetSaleDate(saleDate);
        ProductName = NormalizeRequired(productName, nameof(productName));
        QuantitySold = EnsureNonNegative(quantitySold, nameof(quantitySold));
        DoughBallsUsed = EnsureNonNegative(doughBallsUsed, nameof(doughBallsUsed));
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetSaleDate(DateOnly saleDate)
    {
        if (saleDate == default)
        {
            throw new ArgumentException("Sale date is required.", nameof(saleDate));
        }

        SaleDate = saleDate;
        DayOfWeek = saleDate.DayOfWeek;
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
