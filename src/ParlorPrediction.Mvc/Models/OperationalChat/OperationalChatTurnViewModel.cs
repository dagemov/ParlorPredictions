namespace ParlorPrediction.Mvc.Models.OperationalChat;

public sealed class OperationalChatTurnViewModel
{
    public Guid CorrelationId { get; init; }

    public string UserMessage { get; init; } = string.Empty;

    public string AssistantSummary { get; init; } = string.Empty;

    public bool RequiresClarification { get; init; }

    public string ClarificationPrompt { get; init; } = string.Empty;

    public IReadOnlyList<OperationalChatDetectedIntentViewModel> DetectedIntents { get; init; } = Array.Empty<OperationalChatDetectedIntentViewModel>();

    public IReadOnlyList<OperationalChatCreatedDraftViewModel> CreatedDrafts { get; init; } = Array.Empty<OperationalChatCreatedDraftViewModel>();

    public IReadOnlyList<OperationalChatWarningViewModel> Warnings { get; init; } = Array.Empty<OperationalChatWarningViewModel>();
}

public sealed class OperationalChatDetectedIntentViewModel
{
    public string Domain { get; init; } = string.Empty;

    public string IntentKind { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Decision { get; init; } = string.Empty;

    public DateOnly EffectiveDate { get; init; }

    public bool RequiresClarification { get; init; }

    public string ClarificationPrompt { get; init; } = string.Empty;

    public string SourceFragment { get; init; } = string.Empty;
}

public sealed class OperationalChatCreatedDraftViewModel
{
    public Guid DraftId { get; init; }

    public string DraftType { get; init; } = string.Empty;

    public string DraftTypeDisplay { get; init; } = string.Empty;

    public string DraftStatus { get; init; } = string.Empty;

    public string DraftStatusDisplay { get; init; } = string.Empty;

    public string ReviewPath { get; init; } = string.Empty;

    public string RiskLevel { get; init; } = string.Empty;

    public bool HasConflicts { get; init; }

    public int WarningCount { get; init; }
}

public sealed class OperationalChatWarningViewModel
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool BlocksDraft { get; init; }

    public bool RequiresHumanReview { get; init; }
}
