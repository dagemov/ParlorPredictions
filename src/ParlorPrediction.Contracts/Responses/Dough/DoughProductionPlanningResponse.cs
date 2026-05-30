namespace ParlorPrediction.Contracts.Responses.Dough;

public sealed class DoughProductionPlanningResponse
{
    public DateOnly ProductionDate { get; init; }

    public int TotalFutureRequiredBalls { get; init; }

    public int ReadyBalls { get; init; }

    public int FermentingBalls { get; init; }

    public int UnballedBalls { get; init; }

    public int MissingBallsForProductionWindow { get; init; }

    public int RecommendedCasesToMakeToday { get; init; }

    public int RecommendedLoadsToMakeToday { get; init; }

    public int RecommendedBallsToBallToday { get; init; }

    public IReadOnlyList<DoughNeedByDateResponse> UpcomingNeeds { get; init; } = Array.Empty<DoughNeedByDateResponse>();

    public string Reason { get; init; } = string.Empty;
}
