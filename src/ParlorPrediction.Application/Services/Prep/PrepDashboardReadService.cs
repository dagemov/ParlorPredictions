using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepDashboardReadService : IPrepDashboardReadService
{
    private const int WeeklyWindowDays = 7;

    private readonly IDoughPrepRecommendationReadRepository _doughPrepRecommendationReadRepository;
    private readonly IPrepTaskRepository _prepTaskRepository;
    private readonly IRestaurantEventReadRepository _restaurantEventReadRepository;

    public PrepDashboardReadService(
        IDoughPrepRecommendationReadRepository doughPrepRecommendationReadRepository,
        IPrepTaskRepository prepTaskRepository,
        IRestaurantEventReadRepository restaurantEventReadRepository)
    {
        _doughPrepRecommendationReadRepository = doughPrepRecommendationReadRepository;
        _prepTaskRepository = prepTaskRepository;
        _restaurantEventReadRepository = restaurantEventReadRepository;
    }

    public async Task<PrepDashboardSummaryResponse> GetSummaryAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var weeklyWindowEndDate = targetDate.AddDays(WeeklyWindowDays - 1);
        var recommendation = await _doughPrepRecommendationReadRepository.GetLatestByDateAsync(targetDate, cancellationToken);
        var tasks = await _prepTaskRepository.GetDoughTasksByDateAsync(targetDate, cancellationToken);
        var weeklyRecommendations = await _doughPrepRecommendationReadRepository.GetLatestBetweenDatesAsync(
            targetDate,
            weeklyWindowEndDate,
            cancellationToken);
        var weeklyTasks = await _prepTaskRepository.GetDoughTasksBetweenDatesAsync(
            targetDate,
            weeklyWindowEndDate,
            cancellationToken);
        var weeklyEvents = await _restaurantEventReadRepository.GetBetweenDatesAsync(
            targetDate,
            weeklyWindowEndDate,
            cancellationToken);

        return new PrepDashboardSummaryResponse
        {
            TargetDate = targetDate,
            WeeklyWindowEndDate = weeklyWindowEndDate,
            HasRecommendation = recommendation is not null,
            RequiredBalls = recommendation?.RequiredBalls ?? 0,
            AvailableBalls = recommendation?.AvailableBalls ?? 0,
            MissingBalls = recommendation?.MissingBalls ?? 0,
            RecommendedCases = recommendation?.RecommendedCases ?? 0,
            RecommendedLoads = recommendation?.RecommendedLoads ?? 0,
            PendingTasks = tasks.Count(IsPendingTask),
            CompletedTasks = tasks.Count(IsCompletedTask),
            WeeklyNeededBalls = weeklyRecommendations.Sum(item => item.RequiredBalls),
            WeeklyCoveredBalls = weeklyRecommendations.Sum(item => Math.Max(item.RequiredBalls - item.MissingBalls, 0)),
            WeeklyPendingBalls = weeklyRecommendations.Sum(item => item.MissingBalls),
            WeeklyCompletedTasks = weeklyTasks.Count(IsCompletedTask),
            WeeklyPendingTasks = weeklyTasks.Count(IsPendingTask),
            WeeklyUpcomingEventBalls = weeklyEvents
                .Where(item => item.IsActive)
                .Sum(item => item.EstimatedDoughBalls),
            LastRecommendationReason = recommendation?.Reason,
            LastRecommendationSavedAtUtc = recommendation?.CreatedAtUtc
        };
    }

    private static bool IsCompletedTask(PrepTask task)
    {
        return task.Status == PrepTaskStatus.Completed;
    }

    private static bool IsPendingTask(PrepTask task)
    {
        return task.Status is not PrepTaskStatus.Completed and not PrepTaskStatus.Cancelled;
    }
}
