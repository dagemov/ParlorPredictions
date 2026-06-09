using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Domain.Entities;

public sealed class DoughReballRecord
{
    public const int ManagerNoteMaxLength = 1000;

    private DoughReballRecord()
    {
    }

    private DoughReballRecord(
        Guid id,
        Guid doughBatchQualityRecordId,
        int quantityBeforeReball,
        int quantityRecoveredBalls,
        DateOnly reballDate,
        ReballResult result,
        DateOnly? mustUseByDate,
        string createdByUserId,
        string? managerNote)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetDoughBatchQualityRecordId(doughBatchQualityRecordId);
        SetQuantities(quantityBeforeReball, quantityRecoveredBalls, result);
        SetReballDate(reballDate);
        Result = result;
        MustUseByDate = mustUseByDate;
        CreatedByUserId = NormalizeRequired(createdByUserId, nameof(createdByUserId));
        ManagerNote = NormalizeOptional(managerNote);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid DoughBatchQualityRecordId { get; private set; }

    public int QuantityBeforeReball { get; private set; }

    public int QuantityRecoveredBalls { get; private set; }

    public int QuantityLostBalls { get; private set; }

    public DateOnly ReballDate { get; private set; }

    public ReballResult Result { get; private set; }

    public DateOnly? MustUseByDate { get; private set; }

    public string? ManagerNote { get; private set; }

    public string CreatedByUserId { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DoughBatchQualityRecord DoughBatchQualityRecord { get; private set; } = null!;

    public User CreatedByUser { get; private set; } = null!;

    public static DoughReballRecord Create(
        Guid doughBatchQualityRecordId,
        int quantityBeforeReball,
        int quantityRecoveredBalls,
        DateOnly reballDate,
        ReballResult result,
        string createdByUserId,
        DateOnly? mustUseByDate = null,
        string? managerNote = null,
        Guid? id = null)
    {
        return new DoughReballRecord(
            id ?? Guid.Empty,
            doughBatchQualityRecordId,
            quantityBeforeReball,
            quantityRecoveredBalls,
            reballDate,
            result,
            mustUseByDate,
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

    private void SetQuantities(int quantityBeforeReball, int quantityRecoveredBalls, ReballResult result)
    {
        if (quantityBeforeReball <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityBeforeReball), "Quantity before reball must be greater than zero.");
        }

        if (quantityRecoveredBalls < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityRecoveredBalls), "Recovered quantity cannot be negative.");
        }

        QuantityBeforeReball = quantityBeforeReball;
        QuantityRecoveredBalls = quantityRecoveredBalls;
        QuantityLostBalls = checked(quantityBeforeReball - quantityRecoveredBalls);

        if (QuantityLostBalls < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityRecoveredBalls), "Recovered quantity cannot be greater than the original quantity.");
        }

        switch (result)
        {
            case ReballResult.PartialRecovered when quantityRecoveredBalls <= 0 || quantityRecoveredBalls >= quantityBeforeReball:
                throw new ArgumentOutOfRangeException(nameof(quantityRecoveredBalls), "Partial recovery must be greater than zero and lower than the original quantity.");

            case ReballResult.Discarded when quantityRecoveredBalls != 0:
                throw new ArgumentOutOfRangeException(nameof(quantityRecoveredBalls), "Discarded reball results cannot recover dough.");

            case ReballResult.ManagerCancelled when quantityRecoveredBalls != quantityBeforeReball:
                throw new ArgumentOutOfRangeException(nameof(quantityRecoveredBalls), "Cancelled reball results must preserve the full original quantity.");
        }
    }

    private void SetReballDate(DateOnly reballDate)
    {
        if (reballDate == default)
        {
            throw new ArgumentException("Reball date is required.", nameof(reballDate));
        }

        ReballDate = reballDate;
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
