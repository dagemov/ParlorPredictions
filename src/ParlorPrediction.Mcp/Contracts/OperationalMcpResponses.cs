using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Mcp.Contracts;

public sealed class ReadWeeklyClosingToolResponse
{
    public required IReadOnlyList<WeeklyDoughClosingResponse> Closings { get; init; }

    public required WeeklyDoughCarryoverResponse Carryover { get; init; }
}

public sealed class ExplainWeeklyGoalToolResponse
{
    public required string Explanation { get; init; }

    public required WeeklyDoughCalendarResponse WeeklyGoal { get; init; }

    public required DoughAvailabilityProjectionResponse Availability { get; init; }

    public required DoughInventoryImpactResponse InventoryImpact { get; init; }
}

public sealed class SimulateOperationalNarrativeToolResponse
{
    public Guid CorrelationId { get; init; }

    public string IntentKind { get; init; } = string.Empty;

    public string NormalizedSummary { get; init; } = string.Empty;

    public string BeforeSnapshotJson { get; init; } = string.Empty;

    public string AfterPreviewJson { get; init; } = string.Empty;

    public string DiffJson { get; init; } = string.Empty;

    public string ValidationWarningsJson { get; init; } = string.Empty;
}

public sealed class OperationalDraftToolResponse
{
    public required OperationalDraft Draft { get; init; }

    public required OperationalAuditEntry AuditEntry { get; init; }

    public string DiffJson { get; init; } = string.Empty;
}

public sealed class ValidateClosingBeforeSaveToolResponse
{
    public bool IsValid { get; init; }

    public string ValidationWarningsJson { get; init; } = string.Empty;
}
