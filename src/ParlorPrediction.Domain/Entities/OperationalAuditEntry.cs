namespace ParlorPrediction.Domain.Entities;

public sealed class OperationalAuditEntry
{
    private OperationalAuditEntry(
        Guid id,
        Guid correlationId,
        string eventType,
        string actorUserId,
        string sourceText,
        string normalizedIntentJson,
        string beforeSnapshotJson,
        string afterPreviewJson,
        string validationWarningsJson,
        Guid? draftId,
        Guid? approvedEntityId,
        DateTime createdAtUtc)
    {
        Id = id;
        CorrelationId = correlationId;
        EventType = eventType;
        ActorUserId = actorUserId;
        SourceText = sourceText;
        NormalizedIntentJson = normalizedIntentJson;
        BeforeSnapshotJson = beforeSnapshotJson;
        AfterPreviewJson = afterPreviewJson;
        ValidationWarningsJson = validationWarningsJson;
        DraftId = draftId;
        ApprovedEntityId = approvedEntityId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CorrelationId { get; private set; }

    public string EventType { get; private set; }

    public string ActorUserId { get; private set; }

    public string SourceText { get; private set; }

    public string NormalizedIntentJson { get; private set; }

    public string BeforeSnapshotJson { get; private set; }

    public string AfterPreviewJson { get; private set; }

    public string ValidationWarningsJson { get; private set; }

    public Guid? DraftId { get; private set; }

    public Guid? ApprovedEntityId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static OperationalAuditEntry Create(
        Guid correlationId,
        string eventType,
        string actorUserId,
        string sourceText,
        string normalizedIntentJson,
        string beforeSnapshotJson,
        string afterPreviewJson,
        string validationWarningsJson,
        Guid? draftId,
        Guid? approvedEntityId = null,
        DateTime? createdAtUtc = null)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id is required.", nameof(correlationId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new ArgumentException("Actor user id is required.", nameof(actorUserId));
        }

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            throw new ArgumentException("Source text is required.", nameof(sourceText));
        }

        return new OperationalAuditEntry(
            Guid.NewGuid(),
            correlationId,
            eventType.Trim(),
            actorUserId.Trim(),
            sourceText.Trim(),
            normalizedIntentJson ?? string.Empty,
            beforeSnapshotJson ?? string.Empty,
            afterPreviewJson ?? string.Empty,
            validationWarningsJson ?? string.Empty,
            draftId,
            approvedEntityId,
            createdAtUtc ?? DateTime.UtcNow);
    }
}
