using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepWeeklyDoughCalendarService
{
    Task<WeeklyDoughCalendarResponse> GetWeekAsync(
        DateOnly referenceDate,
        int historicalWeeksToUse,
        CancellationToken cancellationToken = default);
}
