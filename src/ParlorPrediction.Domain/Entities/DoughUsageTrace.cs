using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Domain.Entities;

public sealed class DoughUsageTrace
{
    public const int NotesMaxLength = 1000;

    private DoughUsageTrace()
    {
    }

    private DoughUsageTrace(
        Guid id,
        DateOnly usageDate,
        Guid sourceDoughBatchQualityRecordId,
        DateOnly sourceDate,
        DoughQualityStatus sourceType,
        DoughUsageDestination destination,
        int trayCount,
        string createdByUserId,
        string? notes)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetUsageDate(usageDate);
        SetSourceDoughBatchQualityRecordId(sourceDoughBatchQualityRecordId);
        SetSourceDate(sourceDate);
        SetSourceType(sourceType);
        SetDestination(destination);
        SetTrayCount(trayCount);
        Notes = NormalizeOptional(notes);

        var createdBy = NormalizeRequired(createdByUserId, nameof(createdByUserId));
        CreatedByUserId = createdBy;
        UpdatedByUserId = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly UsageDate { get; private set; }

    public Guid SourceDoughBatchQualityRecordId { get; private set; }

    public DateOnly SourceDate { get; private set; }

    public DoughQualityStatus SourceType { get; private set; }

    public DoughUsageDestination Destination { get; private set; }

    public int TrayCount { get; private set; }

    public int BallsPerTray { get; private set; }

    public int BallsUsed { get; private set; }

    public string? Notes { get; private set; }

    public string CreatedByUserId { get; private set; } = null!;

    public string UpdatedByUserId { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DoughBatchQualityRecord SourceDoughBatchQualityRecord { get; private set; } = null!;

    public User CreatedByUser { get; private set; } = null!;

    public User UpdatedByUser { get; private set; } = null!;

    public static DoughUsageTrace Create(
        DateOnly usageDate,
        Guid sourceDoughBatchQualityRecordId,
        DateOnly sourceDate,
        DoughQualityStatus sourceType,
        DoughUsageDestination destination,
        int trayCount,
        string createdByUserId,
        string? notes = null,
        Guid? id = null)
    {
        return new DoughUsageTrace(
            id ?? Guid.Empty,
            usageDate,
            sourceDoughBatchQualityRecordId,
            sourceDate,
            sourceType,
            destination,
            trayCount,
            createdByUserId,
            notes);
    }

    public void Correct(
        DateOnly usageDate,
        Guid sourceDoughBatchQualityRecordId,
        DateOnly sourceDate,
        DoughQualityStatus sourceType,
        DoughUsageDestination destination,
        int trayCount,
        string updatedByUserId,
        string? notes = null)
    {
        SetUsageDate(usageDate);
        SetSourceDoughBatchQualityRecordId(sourceDoughBatchQualityRecordId);
        SetSourceDate(sourceDate);
        SetSourceType(sourceType);
        SetDestination(destination);
        SetTrayCount(trayCount);
        Notes = NormalizeOptional(notes);
        UpdatedByUserId = NormalizeRequired(updatedByUserId, nameof(updatedByUserId));
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetUsageDate(DateOnly usageDate)
    {
        if (usageDate == default)
        {
            throw new ArgumentException("Usage date is required.", nameof(usageDate));
        }

        UsageDate = usageDate;
    }

    private void SetSourceDoughBatchQualityRecordId(Guid sourceDoughBatchQualityRecordId)
    {
        if (sourceDoughBatchQualityRecordId == Guid.Empty)
        {
            throw new ArgumentException("Source dough quality record id is required.", nameof(sourceDoughBatchQualityRecordId));
        }

        SourceDoughBatchQualityRecordId = sourceDoughBatchQualityRecordId;
    }

    private void SetSourceDate(DateOnly sourceDate)
    {
        if (sourceDate == default)
        {
            throw new ArgumentException("Source date is required.", nameof(sourceDate));
        }

        SourceDate = sourceDate;
    }

    private void SetSourceType(DoughQualityStatus sourceType)
    {
        if (sourceType == DoughQualityStatus.Discarded)
        {
            throw new ArgumentException("Discarded dough cannot be used as a trace source.", nameof(sourceType));
        }

        SourceType = sourceType;
    }

    private void SetDestination(DoughUsageDestination destination)
    {
        Destination = destination;
    }

    private void SetTrayCount(int trayCount)
    {
        if (trayCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trayCount), "Tray count must be greater than zero.");
        }

        TrayCount = trayCount;
        BallsPerTray = DoughRules.BallsPerCase;
        BallsUsed = checked(trayCount * BallsPerTray);
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
