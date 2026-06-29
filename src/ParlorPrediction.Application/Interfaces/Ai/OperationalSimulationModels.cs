using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Interfaces.Ai;

public sealed class WeeklyCorrectionProposal
{
    public Guid? ExistingWeeklyClosingId { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public int NeededBalls { get; init; }

    public int ProducedBalls { get; init; }

    public int UsedBalls { get; init; }

    public int LostBalls { get; init; }

    public int LeftoverReadyBalls { get; init; }

    public int LeftoverAttentionBalls { get; init; }

    public int LeftoverMixedLoads { get; init; }

    public string Notes { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;
}

public sealed class DoughTaskDraftProposal
{
    public DateOnly TaskDate { get; init; }

    public string TaskType { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string QuantityUnit { get; init; } = string.Empty;

    public string AssignedRole { get; init; } = string.Empty;

    public Guid PrepItemId { get; init; }

    public Guid PrepStationId { get; init; }

    public string Notes { get; init; } = string.Empty;

    public bool AutoCompleteOnApproval { get; init; }

    public int? CompletionQuantity { get; init; }
}

public sealed class OperationalValidationWarning
{
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool RequiresHumanReview { get; init; } = true;

    public bool BlocksDraft { get; init; }
}

public sealed class OperationalSimulationResult
{
    public Guid CorrelationId { get; init; }

    public required string SourceText { get; init; }

    public required OperationalIntent Intent { get; init; }

    public string NormalizedSummary => Intent.NormalizedSummary;

    public string BeforeSnapshotJson { get; init; } = string.Empty;

    public string AfterPreviewJson { get; init; } = string.Empty;

    public string DiffJson { get; init; } = string.Empty;

    public string ValidationWarningsJson { get; init; } = string.Empty;

    public IReadOnlyList<OperationalValidationWarning> ValidationWarnings { get; init; } = [];

    public WeeklyCorrectionProposal? WeeklyCorrectionProposal { get; init; }

    public DoughTaskDraftProposal? DoughTaskDraftProposal { get; init; }

    public WeeklyDoughClosingResponse? ExistingWeeklyClosing { get; init; }

    public WeeklyDoughCarryoverResponse? Carryover { get; init; }

    public DoughAvailabilityProjectionResponse? Availability { get; init; }

    public WeeklyDoughCalendarResponse? WeeklyGoal { get; init; }

    public DoughInventoryImpactResponse? InventoryImpact { get; init; }
}

public sealed class WeeklyGoalExplanationResult
{
    public required string Explanation { get; init; }

    public required WeeklyDoughCalendarResponse WeeklyGoal { get; init; }

    public required DoughAvailabilityProjectionResponse Availability { get; init; }

    public required DoughInventoryImpactResponse InventoryImpact { get; init; }
}

public sealed class ClosingValidationResult
{
    public bool IsValid { get; init; }

    public string ValidationWarningsJson { get; init; } = string.Empty;

    public IReadOnlyList<OperationalValidationWarning> ValidationWarnings { get; init; } = [];
}

public sealed class OperationalDraftEnvelope
{
    public required OperationalDraft Draft { get; init; }

    public required OperationalAuditEntry AuditEntry { get; init; }

    public string DiffJson { get; init; } = string.Empty;
}

public sealed class WeeklyCorrectionApprovalPayload
{
    public Guid? ExistingWeeklyClosingId { get; init; }

    public DateOnly WeekStartDate { get; init; }

    public int NeededBalls { get; init; }

    public int ProducedBalls { get; init; }

    public int UsedBalls { get; init; }

    public int LostBalls { get; init; }

    public int LeftoverReadyBalls { get; init; }

    public int LeftoverAttentionBalls { get; init; }

    public int LeftoverMixedLoads { get; init; }

    public string? Notes { get; init; }

    public string CorrectionReason { get; init; } = string.Empty;
}

public sealed class DoughTaskApprovalPayload
{
    public DateOnly TaskDate { get; init; }

    public Guid PrepItemId { get; init; }

    public Guid PrepStationId { get; init; }

    public string AssignedRole { get; init; } = string.Empty;

    public string TaskType { get; init; } = string.Empty;

    public string QuantityUnit { get; init; } = string.Empty;

    public int QuantityValue { get; init; }

    public int? CompletionQuantityValue { get; init; }

    public string? Notes { get; init; }

    public bool AutoCompleteOnApproval { get; init; }
}

public sealed class OperationalDraftApprovalResult
{
    public required OperationalDraft Draft { get; init; }

    public required OperationalAuditEntry AuditEntry { get; init; }

    public Guid? ApprovedEntityId { get; init; }
}
