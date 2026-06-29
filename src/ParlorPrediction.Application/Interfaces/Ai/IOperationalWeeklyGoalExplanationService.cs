namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalWeeklyGoalExplanationService
{
    Task<WeeklyGoalExplanationResult> ExplainAsync(
        DateOnly referenceDate,
        int historicalWeeksToUse,
        CancellationToken cancellationToken = default);
}
