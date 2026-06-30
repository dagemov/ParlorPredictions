namespace ParlorPrediction.Application.Interfaces.Ai;

public abstract record OperationalIntent(
    OperationalIntentKind Kind,
    string SourceText,
    string NormalizedSummary,
    DateOnly ReferenceDate);

public sealed record UnknownIntent(
    string SourceText,
    string NormalizedSummary,
    DateOnly ReferenceDate)
    : OperationalIntent(OperationalIntentKind.Unknown, SourceText, NormalizedSummary, ReferenceDate);

public sealed record SalesIntent(
    string SourceText,
    string NormalizedSummary,
    DateOnly ReferenceDate,
    string? Channel,
    int? RejasSold)
    : OperationalIntent(OperationalIntentKind.Sales, SourceText, NormalizedSummary, ReferenceDate);

public sealed record ProductionIntent(
    string SourceText,
    string NormalizedSummary,
    DateOnly ReferenceDate,
    bool MentionsLoadCreated,
    bool MentionsBalling,
    int? Quantity,
    string? Notes)
    : OperationalIntent(OperationalIntentKind.Production, SourceText, NormalizedSummary, ReferenceDate);

public sealed record ConsumptionIntent(
    string SourceText,
    string NormalizedSummary,
    DateOnly ReferenceDate,
    bool MentionsReball,
    int? BallsUsed,
    string? Notes)
    : OperationalIntent(OperationalIntentKind.Consumption, SourceText, NormalizedSummary, ReferenceDate);

public sealed record InventoryIntent(
    string SourceText,
    string NormalizedSummary,
    DateOnly ReferenceDate,
    int? ReadyBalls,
    int? MixedLoads,
    string? Notes)
    : OperationalIntent(OperationalIntentKind.Inventory, SourceText, NormalizedSummary, ReferenceDate);

public sealed record WeeklyClosingIntent(
    string SourceText,
    string NormalizedSummary,
    DateOnly ReferenceDate,
    DateOnly WeekStartDate,
    int? LinesLeftover,
    int? LeftoverReadyBalls,
    int? LeftoverMixedLoads,
    bool NoPendingLoad,
    bool SundayLoadBalledMonday,
    string CorrectionReason)
    : OperationalIntent(OperationalIntentKind.WeeklyClosing, SourceText, NormalizedSummary, ReferenceDate);
