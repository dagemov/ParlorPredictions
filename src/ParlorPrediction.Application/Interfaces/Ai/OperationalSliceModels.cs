namespace ParlorPrediction.Application.Interfaces.Ai;

public sealed record OperationalUsageComponent(
    string Category,
    int Balls,
    string? ReferenceName = null);

public sealed record OperationalDoughTaskDraftRequest
{
    public Guid? CorrelationId { get; init; }

    public Guid? ExistingPrepTaskId { get; init; }

    public DateOnly TaskDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public string TaskType { get; init; } = string.Empty;

    public int QuantityValue { get; init; }

    public string QuantityUnit { get; init; } = string.Empty;

    public string AssignedRole { get; init; } = string.Empty;

    public bool AutoCompleteOnApproval { get; init; }

    public int? CompletionQuantityValue { get; init; }

    public string? Notes { get; init; }

    public string? ActorUserId { get; init; }
}

public sealed record OperationalDailyClosingDraftRequest
{
    public Guid? CorrelationId { get; init; }

    public DateOnly ClosingDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public int ActualUsedBalls { get; init; }

    public IReadOnlyList<OperationalUsageComponent> UsageBreakdown { get; init; } = Array.Empty<OperationalUsageComponent>();

    public string? Notes { get; init; }

    public string? ActorUserId { get; init; }
}

public sealed record OperationalEventDraftRequest
{
    public Guid? CorrelationId { get; init; }

    public DateOnly EventDate { get; init; }

    public string Name { get; init; } = string.Empty;

    public int EstimatedDoughBalls { get; init; }

    public int ExpectedPeopleMinimum { get; init; }

    public int ExpectedPeopleMaximum { get; init; }

    public int? PreviousNarrativeDoughBalls { get; init; }

    public bool AllowShortFermentation { get; init; }

    public string? Notes { get; init; }

    public string? ActorUserId { get; init; }
}

public sealed record OperationalWeeklyClosingPreviewRequest
{
    public Guid? CorrelationId { get; init; }

    public DateOnly ReferenceDate { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public string? Notes { get; init; }

    public string? ActorUserId { get; init; }
}

public sealed record OperationalWeekSliceRequest
{
    public Guid? CorrelationId { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public DateOnly ReferenceDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public string ActorUserId { get; init; } = string.Empty;

    public IReadOnlyList<OperationalDoughTaskDraftRequest> ProductionDrafts { get; init; } = Array.Empty<OperationalDoughTaskDraftRequest>();

    public IReadOnlyList<OperationalDailyClosingDraftRequest> DailyClosingDrafts { get; init; } = Array.Empty<OperationalDailyClosingDraftRequest>();

    public IReadOnlyList<OperationalEventDraftRequest> EventDrafts { get; init; } = Array.Empty<OperationalEventDraftRequest>();

    public string? WeeklyClosingNotes { get; init; }
}

public sealed class OperationalWeekSliceResult
{
    public Guid CorrelationId { get; init; }

    public IReadOnlyList<OperationalDraftEnvelope> ProductionDrafts { get; init; } = Array.Empty<OperationalDraftEnvelope>();

    public IReadOnlyList<OperationalDraftEnvelope> DailyClosingDrafts { get; init; } = Array.Empty<OperationalDraftEnvelope>();

    public IReadOnlyList<OperationalDraftEnvelope> EventDrafts { get; init; } = Array.Empty<OperationalDraftEnvelope>();

    public required OperationalDraftEnvelope WeeklyClosingDraft { get; init; }

    public IReadOnlyList<OperationalValidationWarning> ValidationWarnings { get; init; } = Array.Empty<OperationalValidationWarning>();
}
