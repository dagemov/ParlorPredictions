namespace ParlorPrediction.Mvc.Models.OperationalDrafts;

public sealed class OperationalDraftDetailsPageViewModel
{
    public Guid DraftId { get; init; }

    public string DraftType { get; init; } = string.Empty;

    public string DraftTypeDisplay { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string StatusDisplay { get; init; } = string.Empty;

    public string CorrelationId { get; init; } = string.Empty;

    public string CreatedBy { get; init; } = string.Empty;

    public DateTime CreatedAtLocal { get; init; }

    public string? ReviewedByUserId { get; init; }

    public DateTime? ReviewedAtLocal { get; init; }

    public string? StatusReason { get; init; }

    public string HumanNarrative { get; init; } = string.Empty;

    public string NormalizedInterpretationJson { get; init; } = string.Empty;

    public string BeforeSnapshotJson { get; init; } = string.Empty;

    public string AfterPreviewJson { get; init; } = string.Empty;

    public IReadOnlyList<OperationalAuditEntryViewModel> AuditEntries { get; init; } = Array.Empty<OperationalAuditEntryViewModel>();
}

public sealed class OperationalAuditEntryViewModel
{
    public string ActionType { get; init; } = string.Empty;

    public string ActorUserId { get; init; } = string.Empty;

    public DateTime TimestampLocal { get; init; }

    public string? ApprovedEntityId { get; init; }

    public bool IsForCurrentDraft { get; init; }
}
