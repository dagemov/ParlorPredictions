using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Domain.Entities;

public sealed class DoughBatchQualityRecord
{
    public const int StatusReasonMaxLength = 500;
    public const int ManagerNoteMaxLength = 1000;

    private DoughBatchQualityRecord()
    {
    }

    private DoughBatchQualityRecord(
        Guid id,
        DateOnly sourceDate,
        Guid? originalDoughTaskId,
        DateTime createdOrBalledAt,
        int quantityBalls,
        DoughQualityStatus initialStatus,
        string? statusReason,
        DateOnly? mustUseByDate,
        DoughLossReason? discardReason,
        string? managerNote,
        string createdByUserId)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetSourceDate(sourceDate);
        SetOriginalDoughTask(originalDoughTaskId);
        SetCreatedOrBalledAt(createdOrBalledAt);
        SetQuantityBalls(quantityBalls);

        var createdBy = NormalizeRequired(createdByUserId, nameof(createdByUserId));
        CreatedByUserId = createdBy;
        UpdatedByUserId = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;

        StatusReason = NormalizeOptional(statusReason);
        ManagerNote = NormalizeOptional(managerNote);

        ApplyInitialStatus(initialStatus, mustUseByDate, discardReason);
    }

    public Guid Id { get; private set; }

    public DateOnly SourceDate { get; private set; }

    public Guid? OriginalDoughTaskId { get; private set; }

    public DateTime CreatedOrBalledAt { get; private set; }

    public int QuantityBalls { get; private set; }

    public DoughQualityStatus CurrentStatus { get; private set; }

    public string? StatusReason { get; private set; }

    public DateTime? AttentionMarkedAt { get; private set; }

    public DateTime? ReballedAt { get; private set; }

    public DateOnly? MustUseByDate { get; private set; }

    public DateTime? DiscardedAt { get; private set; }

    public DoughLossReason? DiscardReason { get; private set; }

    public string? ManagerNote { get; private set; }

    public string CreatedByUserId { get; private set; } = null!;

    public string UpdatedByUserId { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public PrepTask? OriginalDoughTask { get; private set; }

    public User CreatedByUser { get; private set; } = null!;

    public User UpdatedByUser { get; private set; } = null!;

    public ICollection<DoughLossRecord> LossRecords { get; private set; } = new List<DoughLossRecord>();

    public ICollection<DoughReballRecord> ReballRecords { get; private set; } = new List<DoughReballRecord>();

    public ICollection<DoughUsageTrace> UsageTraces { get; private set; } = new List<DoughUsageTrace>();

    public bool CountsAsAvailable => DoughQualityRules.CountsAsAvailable(CurrentStatus);

    public static DoughBatchQualityRecord Create(
        DateOnly sourceDate,
        DateTime createdOrBalledAt,
        int quantityBalls,
        string createdByUserId,
        Guid? originalDoughTaskId = null,
        DoughQualityStatus initialStatus = DoughQualityStatus.Good,
        string? statusReason = null,
        DateOnly? mustUseByDate = null,
        DoughLossReason? discardReason = null,
        string? managerNote = null,
        Guid? id = null)
    {
        return new DoughBatchQualityRecord(
            id ?? Guid.Empty,
            sourceDate,
            originalDoughTaskId,
            createdOrBalledAt,
            quantityBalls,
            initialStatus,
            statusReason,
            mustUseByDate,
            discardReason,
            managerNote,
            createdByUserId);
    }

    public void MarkAttention(
        DateTime attentionMarkedAtUtc,
        string statusReason,
        string updatedByUserId,
        string? managerNote = null)
    {
        EnsureNotDiscarded("Discarded dough cannot be moved back to attention.");

        CurrentStatus = DoughQualityStatus.Attention;
        AttentionMarkedAt = EnsureTimestamp(attentionMarkedAtUtc, nameof(attentionMarkedAtUtc));
        StatusReason = NormalizeRequired(statusReason, nameof(statusReason));

        UpdateAudit(updatedByUserId, managerNote);
    }

    public void CorrectStatus(
        DoughQualityStatus newStatus,
        string updatedByUserId,
        string? statusReason = null,
        string? managerNote = null,
        DateTime? effectiveAtUtc = null,
        DateOnly? mustUseByDate = null,
        DoughLossReason? discardReason = null)
    {
        var effectiveAt = effectiveAtUtc ?? DateTime.UtcNow;
        EnsureTimestamp(effectiveAt, nameof(effectiveAtUtc));

        CurrentStatus = newStatus;

        if (!string.IsNullOrWhiteSpace(statusReason))
        {
            StatusReason = NormalizeOptional(statusReason);
        }

        if (!string.IsNullOrWhiteSpace(managerNote))
        {
            ManagerNote = NormalizeOptional(managerNote);
        }

        switch (newStatus)
        {
            case DoughQualityStatus.Attention:
                AttentionMarkedAt ??= effectiveAt;
                ClearDiscardState();
                MustUseByDate = null;
                break;

            case DoughQualityStatus.Reballed:
                ReballedAt ??= effectiveAt;
                ClearDiscardState();
                MustUseByDate = null;
                break;

            case DoughQualityStatus.MustUseNextDay:
                ReballedAt ??= effectiveAt;
                MustUseByDate = mustUseByDate ?? DoughQualityRules.BuildMustUseByDate(effectiveAt);
                ClearDiscardState();
                break;

            case DoughQualityStatus.Discarded:
                DiscardReason = discardReason
                    ?? throw new ArgumentException("Discard reason is required when correcting a dough record to discarded.", nameof(discardReason));
                DiscardedAt = effectiveAt;
                MustUseByDate = null;
                break;

            default:
                ClearDiscardState();
                MustUseByDate = null;
                break;
        }

        UpdateAudit(updatedByUserId);
    }

    public void ApplyPartialReball(
        int quantityRecoveredBalls,
        DateTime reballDateUtc,
        string updatedByUserId,
        string? managerNote = null)
    {
        EnsureNotDiscarded("Discarded dough cannot be re-balled.");

        var reballDate = EnsureTimestamp(reballDateUtc, nameof(reballDateUtc));
        var currentQuantity = QuantityBalls;

        if (quantityRecoveredBalls <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityRecoveredBalls), "Recovered quantity must be greater than zero.");
        }

        if (quantityRecoveredBalls >= currentQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityRecoveredBalls), "Reball recovery must be partial and less than the current dough quantity.");
        }

        QuantityBalls = quantityRecoveredBalls;
        CurrentStatus = DoughQualityStatus.MustUseNextDay;
        ReballedAt = reballDate;
        MustUseByDate = DoughQualityRules.BuildMustUseByDate(reballDate);
        StatusReason = NormalizeOptional("Partial reball recovery");
        ClearDiscardState();

        UpdateAudit(updatedByUserId, managerNote);
    }

    public void Discard(
        DoughLossReason discardReason,
        DateTime discardedAtUtc,
        string updatedByUserId,
        string? managerNote = null)
    {
        if (CurrentStatus == DoughQualityStatus.Discarded)
        {
            throw new InvalidOperationException("This dough quality record is already discarded.");
        }

        CurrentStatus = DoughQualityStatus.Discarded;
        DiscardReason = discardReason;
        DiscardedAt = EnsureTimestamp(discardedAtUtc, nameof(discardedAtUtc));
        MustUseByDate = null;

        UpdateAudit(updatedByUserId, managerNote);
    }

    private void ApplyInitialStatus(
        DoughQualityStatus initialStatus,
        DateOnly? mustUseByDate,
        DoughLossReason? discardReason)
    {
        CurrentStatus = initialStatus;

        switch (initialStatus)
        {
            case DoughQualityStatus.Attention:
                AttentionMarkedAt = CreatedAtUtc;
                break;

            case DoughQualityStatus.Reballed:
                ReballedAt = CreatedAtUtc;
                break;

            case DoughQualityStatus.MustUseNextDay:
                ReballedAt = CreatedAtUtc;
                MustUseByDate = mustUseByDate ?? DoughQualityRules.BuildMustUseByDate(CreatedAtUtc);
                break;

            case DoughQualityStatus.Discarded:
                DiscardReason = discardReason
                    ?? throw new ArgumentException("Discard reason is required when creating a discarded dough quality record.", nameof(discardReason));
                DiscardedAt = CreatedAtUtc;
                break;
        }
    }

    private void SetSourceDate(DateOnly sourceDate)
    {
        if (sourceDate == default)
        {
            throw new ArgumentException("Source date is required.", nameof(sourceDate));
        }

        SourceDate = sourceDate;
    }

    private void SetOriginalDoughTask(Guid? originalDoughTaskId)
    {
        if (originalDoughTaskId == Guid.Empty)
        {
            throw new ArgumentException("Original dough task id cannot be an empty guid.", nameof(originalDoughTaskId));
        }

        OriginalDoughTaskId = originalDoughTaskId;
    }

    private void SetCreatedOrBalledAt(DateTime createdOrBalledAt)
    {
        CreatedOrBalledAt = EnsureTimestamp(createdOrBalledAt, nameof(createdOrBalledAt));
    }

    private void SetQuantityBalls(int quantityBalls)
    {
        if (quantityBalls <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityBalls), "Quantity balls must be greater than zero.");
        }

        QuantityBalls = quantityBalls;
    }

    private void UpdateAudit(string updatedByUserId, string? managerNote = null)
    {
        UpdatedByUserId = NormalizeRequired(updatedByUserId, nameof(updatedByUserId));

        if (managerNote is not null)
        {
            ManagerNote = NormalizeOptional(managerNote);
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureNotDiscarded(string message)
    {
        if (CurrentStatus == DoughQualityStatus.Discarded)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void ClearDiscardState()
    {
        DiscardReason = null;
        DiscardedAt = null;
    }

    private static DateTime EnsureTimestamp(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("Timestamp is required.", parameterName);
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
