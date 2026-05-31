namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughProductionPlanningViewModel
{
    public DateOnly ProductionDate { get; set; }

    public int DaysAhead { get; set; } = 7;

    public int TotalFutureRequiredBalls { get; set; }

    public int ReadyBalls { get; set; }

    public int FermentingBalls { get; set; }

    public int UnballedBalls { get; set; }

    public int MissingBallsForProductionWindow { get; set; }

    public int RecommendedCasesToMakeToday { get; set; }

    public int RecommendedLoadsToMakeToday { get; set; }

    public int RecommendedBallsToBallToday { get; set; }

    public string Reason { get; set; } = string.Empty;

    public IReadOnlyList<DoughNeedByDateViewModel> UpcomingNeeds { get; set; } = Array.Empty<DoughNeedByDateViewModel>();

    public bool HasUpcomingNeeds => UpcomingNeeds.Count > 0;

    public bool HasProductionWorkToday => RecommendedCasesToMakeToday > 0 || RecommendedBallsToBallToday > 0;
}
