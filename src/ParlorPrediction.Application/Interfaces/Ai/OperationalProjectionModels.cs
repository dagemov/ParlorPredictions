namespace ParlorPrediction.Application.Interfaces.Ai;

public sealed record OperationalProjectionRequest
{
    public Guid? CorrelationId { get; init; }

    public DateOnly ReferenceDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public string? Notes { get; init; }

    public string? ActorUserId { get; init; }
}

public sealed class ProductionLedgerSummary
{
    public int EntryCount { get; init; }

    public int TotalBallsCreated { get; init; }

    public int BallsCompleted { get; init; }

    public int BallsReballed { get; init; }

    public int BallsDiscarded { get; init; }
}

public sealed class ConsumptionLedgerSummary
{
    public int EntryCount { get; init; }

    public int SalesBalls { get; init; }

    public int EventBalls { get; init; }

    public int ServiceUsageBalls { get; init; }

    public int PotentialEventDoubleCountBalls { get; init; }
}

public sealed class InventoryTransformationLedgerSummary
{
    public int EntryCount { get; init; }

    public int BallsRecovered { get; init; }

    public int BallsDiscarded { get; init; }

    public int BallsReclassified { get; init; }
}

public sealed class OperationalProjectionDayView
{
    public DateOnly Date { get; init; }

    public int ForecastNeededBalls { get; init; }

    public int? ActualUsedBalls { get; init; }

    public int RemainingDemandBalls { get; init; }

    public bool IsClosed { get; init; }

    public bool IsToday { get; init; }

    public bool IsFuture { get; init; }

    public string? Notes { get; init; }
}

public sealed class OperationalProjectionResult
{
    public Guid CorrelationId { get; init; }

    public DateOnly ReferenceDate { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public DateOnly WeekEndDate { get; init; }

    public int ReadyNowBalls { get; init; }

    public int BallsReadyForService { get; init; }

    public int FutureBalls { get; init; }

    public int MixedButNotBalledBalls { get; init; }

    public int StillFermentingBalls { get; init; }

    public int RemainingWeekDemandBalls { get; init; }

    public int ProjectedCoverageBalls { get; init; }

    public int ProjectedShortageBalls { get; init; }

    public int ProjectedSurplusBalls { get; init; }

    public bool WeeklyClosingUsageConsistent { get; init; }

    public ProductionLedgerSummary ProductionLedger { get; init; } = new();

    public ConsumptionLedgerSummary ConsumptionLedger { get; init; } = new();

    public InventoryTransformationLedgerSummary InventoryTransformationLedger { get; init; } = new();

    public IReadOnlyList<OperationalProjectionDayView> Days { get; init; } = Array.Empty<OperationalProjectionDayView>();

    public IReadOnlyList<OperationalValidationWarning> ValidationWarnings { get; init; } = Array.Empty<OperationalValidationWarning>();
}

public sealed class ProjectionAdjustmentDraftProposal
{
    public DateOnly ReferenceDate { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public DateOnly WeekEndDate { get; init; }

    public int ReadyNowBalls { get; init; }

    public int BallsReadyForService { get; init; }

    public int RemainingWeekDemandBalls { get; init; }

    public int ProjectedCoverageBalls { get; init; }

    public int ProjectedShortageBalls { get; init; }

    public int SuggestedAdditionalBallDoughBalls { get; init; }

    public int SuggestedAdditionalMakeDoughLoads { get; init; }

    public string Notes { get; init; } = string.Empty;
}

public sealed class ProjectionAdjustmentDraftPayload
{
    public DateOnly ReferenceDate { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public DateOnly WeekEndDate { get; init; }

    public int ReadyNowBalls { get; init; }

    public int BallsReadyForService { get; init; }

    public int RemainingWeekDemandBalls { get; init; }

    public int ProjectedCoverageBalls { get; init; }

    public int ProjectedShortageBalls { get; init; }

    public int SuggestedAdditionalBallDoughBalls { get; init; }

    public int SuggestedAdditionalMakeDoughLoads { get; init; }

    public string? Notes { get; init; }
}
