using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Domain.Entities;

public sealed class OperationalDraft
{
    public const int DraftTypeMaxLength = 100;
    public const int UserIdMaxLength = 450;
    public const int StatusReasonMaxLength = 1000;

    private OperationalDraft()
    {
    }

    private OperationalDraft(
        Guid id,
        Guid correlationId,
        string draftType,
        string sourceText,
        string normalizedIntentJson,
        string beforeSnapshotJson,
        string afterPreviewJson,
        string validationWarningsJson,
        string draftPayloadJson,
        OperationalDraftStatus status,
        string createdBy,
        DateTime createdAtUtc)
    {
        Id = id;
        CorrelationId = correlationId;
        DraftType = draftType;
        SourceText = sourceText;
        NormalizedIntentJson = normalizedIntentJson;
        BeforeSnapshotJson = beforeSnapshotJson;
        AfterPreviewJson = afterPreviewJson;
        ValidationWarningsJson = validationWarningsJson;
        DraftPayloadJson = draftPayloadJson;
        Status = status;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CorrelationId { get; private set; }

    public string DraftType { get; private set; }

    public string SourceText { get; private set; }

    public string NormalizedIntentJson { get; private set; }

    public string BeforeSnapshotJson { get; private set; }

    public string AfterPreviewJson { get; private set; }

    public string ValidationWarningsJson { get; private set; }

    public string DraftPayloadJson { get; private set; }

    public OperationalDraftStatus Status { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? ReviewedByUserId { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public string? StatusReason { get; private set; }

    public Guid? ApprovedEntityId { get; private set; }

    public static OperationalDraft Create(
        Guid correlationId,
        string draftType,
        string sourceText,
        string normalizedIntentJson,
        string beforeSnapshotJson,
        string afterPreviewJson,
        string validationWarningsJson,
        string draftPayloadJson,
        string createdBy,
        DateTime? createdAtUtc = null)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id is required.", nameof(correlationId));
        }

        if (string.IsNullOrWhiteSpace(draftType))
        {
            throw new ArgumentException("Draft type is required.", nameof(draftType));
        }

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            throw new ArgumentException("Source text is required.", nameof(sourceText));
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdBy));
        }

        return new OperationalDraft(
            Guid.NewGuid(),
            correlationId,
            draftType.Trim(),
            sourceText.Trim(),
            normalizedIntentJson ?? string.Empty,
            beforeSnapshotJson ?? string.Empty,
            afterPreviewJson ?? string.Empty,
            validationWarningsJson ?? string.Empty,
            draftPayloadJson ?? string.Empty,
            OperationalDraftStatus.Pending,
            createdBy.Trim(),
            createdAtUtc ?? DateTime.UtcNow);
    }

    public void MarkAsReadyForApproval()
    {
        EnsureStatusTransitionAllowed(OperationalDraftStatus.ReadyForApproval);
        Status = OperationalDraftStatus.ReadyForApproval;
        ReviewedAtUtc = DateTime.UtcNow;
        StatusReason = null;
        ApprovedEntityId = null;
    }

    public void Approve(string reviewedByUserId, Guid? approvedEntityId = null, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(reviewedByUserId))
        {
            throw new ArgumentException("Reviewed by user id is required.", nameof(reviewedByUserId));
        }

        if (Status is not OperationalDraftStatus.Pending and not OperationalDraftStatus.ReadyForApproval)
        {
            throw new InvalidOperationException("Only pending drafts can be approved.");
        }

        Status = OperationalDraftStatus.Approved;
        ReviewedByUserId = reviewedByUserId.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
        ApprovedEntityId = approvedEntityId;
        StatusReason = NormalizeReason(reason);
    }

    public void Reject(string reason, string? reviewedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A rejection reason is required.", nameof(reason));
        }

        if (Status == OperationalDraftStatus.Approved)
        {
            throw new InvalidOperationException("Approved drafts cannot be rejected.");
        }

        Status = OperationalDraftStatus.Rejected;
        ReviewedByUserId = string.IsNullOrWhiteSpace(reviewedByUserId)
            ? ReviewedByUserId
            : reviewedByUserId.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
        StatusReason = NormalizeReason(reason);
    }

    private void EnsureStatusTransitionAllowed(OperationalDraftStatus nextStatus)
    {
        if (Status == OperationalDraftStatus.Approved)
        {
            throw new InvalidOperationException("Approved drafts cannot change status.");
        }

        if (Status == OperationalDraftStatus.Rejected && nextStatus != OperationalDraftStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected drafts cannot return to approval flow.");
        }
    }

    private static string? NormalizeReason(string? reason)
    {
        var normalized = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= StatusReasonMaxLength
            ? normalized
            : normalized[..StatusReasonMaxLength];
    }
}
