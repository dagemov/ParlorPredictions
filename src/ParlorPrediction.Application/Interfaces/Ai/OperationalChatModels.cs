namespace ParlorPrediction.Application.Interfaces.Ai;

public sealed class OperationalChatRequest
{
    public Guid? CorrelationId { get; init; }

    public required string SourceText { get; init; }

    public DateOnly ReferenceDate { get; init; }

    public DateOnly? TargetWeekStartDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public string? ActorUserId { get; init; }
}

public sealed class OperationalChatDetectedIntent
{
    public string Domain { get; init; } = string.Empty;

    public string IntentKind { get; init; } = string.Empty;

    public string SourceFragment { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Decision { get; init; } = string.Empty;

    public DateOnly EffectiveDate { get; init; }

    public bool RequiresClarification { get; init; }

    public string ClarificationPrompt { get; init; } = string.Empty;
}

public sealed class OperationalChatCreatedDraft
{
    public Guid DraftId { get; init; }

    public Guid CorrelationId { get; init; }

    public string DraftType { get; init; } = string.Empty;

    public string DraftStatus { get; init; } = string.Empty;

    public string ReviewPath { get; init; } = string.Empty;

    public string RiskLevel { get; init; } = string.Empty;

    public bool HasConflicts { get; init; }

    public IReadOnlyList<OperationalValidationWarning> ValidationWarnings { get; init; } = [];
}

public sealed class OperationalChatResponse
{
    public Guid CorrelationId { get; init; }

    public string SourceText { get; init; } = string.Empty;

    public string NarrativeSummary { get; init; } = string.Empty;

    public bool RequiresClarification { get; init; }

    public string ClarificationPrompt { get; init; } = string.Empty;

    public IReadOnlyList<OperationalChatDetectedIntent> DetectedIntents { get; init; } = [];

    public IReadOnlyList<OperationalChatCreatedDraft> CreatedDrafts { get; init; } = [];

    public IReadOnlyList<OperationalValidationWarning> Warnings { get; init; } = [];
}
