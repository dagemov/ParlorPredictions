namespace ParlorPrediction.Application.Interfaces.Ai;

public sealed class OperationalNarrativeRequest
{
    public Guid? CorrelationId { get; init; }

    public required string SourceText { get; init; }

    public DateOnly ReferenceDate { get; init; }

    public DateOnly? TargetWeekStartDate { get; init; }

    public int HistoricalWeeksToUse { get; init; } = 8;

    public string? ActorUserId { get; init; }
}
