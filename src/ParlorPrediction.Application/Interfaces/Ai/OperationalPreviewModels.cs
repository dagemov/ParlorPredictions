namespace ParlorPrediction.Application.Interfaces.Ai;

public sealed class PreviewState
{
    public int ReadyNowBalls { get; init; }

    public int WeeklyUsedBalls { get; init; }

    public int ProductionBalls { get; init; }

    public int ExternalEventConsumption { get; init; }

    public int BallsReadyForService { get; init; }
}

public sealed class PreviewDiff
{
    public int ReadyNowDelta { get; init; }

    public int WeeklyUsedDelta { get; init; }

    public int ProductionDelta { get; init; }

    public int ExternalEventConsumptionDelta { get; init; }

    public int BallsReadyForServiceDelta { get; init; }

    public IReadOnlyList<string> Changes { get; init; } = Array.Empty<string>();
}

public sealed class OperationalPreviewResult
{
    public Guid DraftId { get; init; }

    public Guid CorrelationId { get; init; }

    public PreviewState Before { get; init; } = new();

    public PreviewState After { get; init; } = new();

    public PreviewDiff Diff { get; init; } = new();

    public IReadOnlyList<OperationalValidationWarning> ValidationWarnings { get; init; } = Array.Empty<OperationalValidationWarning>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public bool HasConflicts { get; init; }

    public string RiskLevel { get; init; } = "Low";

    public bool UsedPersistedSnapshot { get; init; }

    public bool StateDriftDetected { get; init; }
}
