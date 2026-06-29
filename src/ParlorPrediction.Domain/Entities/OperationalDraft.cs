namespace ParlorPrediction.Domain.Entities;

public sealed class OperationalDraft
{
    private OperationalDraft(
        Guid id,
        string draftType,
        string sourceText,
        string normalizedIntentJson,
        string beforeSnapshotJson,
        string afterPreviewJson,
        string validationWarningsJson,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        Id = id;
        DraftType = draftType;
        SourceText = sourceText;
        NormalizedIntentJson = normalizedIntentJson;
        BeforeSnapshotJson = beforeSnapshotJson;
        AfterPreviewJson = afterPreviewJson;
        ValidationWarningsJson = validationWarningsJson;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string DraftType { get; private set; }

    public string SourceText { get; private set; }

    public string NormalizedIntentJson { get; private set; }

    public string BeforeSnapshotJson { get; private set; }

    public string AfterPreviewJson { get; private set; }

    public string ValidationWarningsJson { get; private set; }

    public string CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static OperationalDraft Create(
        string draftType,
        string sourceText,
        string normalizedIntentJson,
        string beforeSnapshotJson,
        string afterPreviewJson,
        string validationWarningsJson,
        string createdByUserId,
        DateTime? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(draftType))
        {
            throw new ArgumentException("Draft type is required.", nameof(draftType));
        }

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            throw new ArgumentException("Source text is required.", nameof(sourceText));
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        }

        return new OperationalDraft(
            Guid.NewGuid(),
            draftType.Trim(),
            sourceText.Trim(),
            normalizedIntentJson ?? string.Empty,
            beforeSnapshotJson ?? string.Empty,
            afterPreviewJson ?? string.Empty,
            validationWarningsJson ?? string.Empty,
            createdByUserId.Trim(),
            createdAtUtc ?? DateTime.UtcNow);
    }
}
