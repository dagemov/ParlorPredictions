namespace ParlorPrediction.Mcp.Contracts;

public class SimulateOperationalNarrativeToolRequest
{
    public Guid? CorrelationId { get; init; }

    public required string SourceText { get; init; }

    public DateOnly ReferenceDate { get; init; }

    public DateOnly? TargetWeekStartDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public string? ActorUserId { get; init; }
}

public sealed class ReadWeeklyClosingToolRequest
{
    public DateOnly? ReferenceDate { get; init; }

    public DateOnly? FromWeekStartDate { get; init; }

    public DateOnly? ToWeekStartDate { get; init; }
}

public sealed class ReadDoughInventoryToolRequest
{
    public DateOnly ReferenceDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;
}

public sealed class ExplainWeeklyGoalToolRequest
{
    public DateOnly ReferenceDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;
}

public sealed class DraftWeeklyCorrectionToolRequest : SimulateOperationalNarrativeToolRequest;

public sealed class DraftDoughTaskToolRequest : SimulateOperationalNarrativeToolRequest;

public sealed class ValidateClosingBeforeSaveToolRequest : SimulateOperationalNarrativeToolRequest;
