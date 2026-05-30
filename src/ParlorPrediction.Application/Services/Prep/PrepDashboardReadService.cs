using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepDashboardReadService : IPrepDashboardReadService
{
    private readonly IDoughPrepRecommendationReadService _doughPrepRecommendationReadService;
    private readonly IPrepTaskReadService _prepTaskReadService;

    public PrepDashboardReadService(
        IDoughPrepRecommendationReadService doughPrepRecommendationReadService,
        IPrepTaskReadService prepTaskReadService)
    {
        _doughPrepRecommendationReadService = doughPrepRecommendationReadService;
        _prepTaskReadService = prepTaskReadService;
    }

    public async Task<PrepDashboardSummaryResponse> GetSummaryAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var recommendation = await _doughPrepRecommendationReadService.GetLatestByDateAsync(targetDate, cancellationToken);
        var tasks = await _prepTaskReadService.GetDoughTasksByDateAsync(targetDate, cancellationToken);

        return new PrepDashboardSummaryResponse
        {
            TargetDate = targetDate,
            HasRecommendation = recommendation is not null,
            RequiredBalls = recommendation?.RequiredBalls ?? 0,
            AvailableBalls = recommendation?.AvailableBalls ?? 0,
            MissingBalls = recommendation?.MissingBalls ?? 0,
            RecommendedCases = recommendation?.RecommendedCases ?? 0,
            RecommendedLoads = recommendation?.RecommendedLoads ?? 0,
            PendingTasks = tasks.Count(IsPendingTask),
            CompletedTasks = tasks.Count(IsCompletedTask),
            LastRecommendationReason = recommendation?.Reason,
            LastRecommendationSavedAtUtc = recommendation?.CreatedAtUtc
        };
    }

    private static bool IsCompletedTask(DoughTaskListItemResponse task)
    {
        return string.Equals(
            task.Status,
            nameof(PrepTaskStatus.Completed),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPendingTask(DoughTaskListItemResponse task)
    {
        return !string.Equals(
                task.Status,
                nameof(PrepTaskStatus.Completed),
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                task.Status,
                nameof(PrepTaskStatus.Cancelled),
                StringComparison.OrdinalIgnoreCase);
    }
}
