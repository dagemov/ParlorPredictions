using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Domain.Entities;

public sealed class DoughLossRecord
{
    public const int ManagerNoteMaxLength = 1000;

    private DoughLossRecord()
    {
    }

    private DoughLossRecord(
        Guid id,
        Guid doughBatchQualityRecordId,
        int quantityLostBalls,
        DoughLossReason lossReason,
        DateOnly lossDate,
        string createdByUserId,
        string? managerNote)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetDoughBatchQualityRecordId(doughBatchQualityRecordId);
        SetQuantityLostBalls(quantityLostBalls);
        LossReason = lossReason;
        SetLossDate(lossDate);
        CreatedByUserId = NormalizeRequired(createdByUserId, nameof(createdByUserId));
        ManagerNote = NormalizeOptional(managerNote);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid DoughBatchQualityRecordId { get; private set; }

    public int QuantityLostBalls { get; private set; }

    public DoughLossReason LossReason { get; private set; }

    public DateOnly LossDate { get; private set; }

    public string? ManagerNote { get; private set; }

    public string CreatedByUserId { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DoughBatchQualityRecord DoughBatchQualityRecord { get; private set; } = null!;

    public User CreatedByUser { get; private set; } = null!;

    public static DoughLossRecord Create(
        Guid doughBatchQualityRecordId,
        int quantityLostBalls,
        DoughLossReason lossReason,
        DateOnly lossDate,
        string createdByUserId,
        string? managerNote = null,
        Guid? id = null)
    {
        return new DoughLossRecord(
            id ?? Guid.Empty,
            doughBatchQualityRecordId,
            quantityLostBalls,
            lossReason,
            lossDate,
            createdByUserId,
            managerNote);
    }

    private void SetDoughBatchQualityRecordId(Guid doughBatchQualityRecordId)
    {
        if (doughBatchQualityRecordId == Guid.Empty)
        {
            throw new ArgumentException("Dough quality record id is required.", nameof(doughBatchQualityRecordId));
        }

        DoughBatchQualityRecordId = doughBatchQualityRecordId;
    }

    private void SetQuantityLostBalls(int quantityLostBalls)
    {
        if (quantityLostBalls <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityLostBalls), "Lost quantity must be greater than zero.");
        }

        QuantityLostBalls = quantityLostBalls;
    }

    private void SetLossDate(DateOnly lossDate)
    {
        if (lossDate == default)
        {
            throw new ArgumentException("Loss date is required.", nameof(lossDate));
        }

        LossDate = lossDate;
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
