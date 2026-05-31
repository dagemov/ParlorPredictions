using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepWeeklyDoughCalendarService : IPrepWeeklyDoughCalendarService
{
    private const int OperationalDays = 6;

    private readonly IDoughPrepCalculationService _doughPrepCalculationService;

    public PrepWeeklyDoughCalendarService(IDoughPrepCalculationService doughPrepCalculationService)
    {
        _doughPrepCalculationService = doughPrepCalculationService;
    }

    public async Task<WeeklyDoughCalendarResponse> GetWeekAsync(
        DateOnly referenceDate,
        int historicalWeeksToUse,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(referenceDate));
        }

        if (historicalWeeksToUse < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(historicalWeeksToUse), "Historical weeks must be at least 1.");
        }

        var weekStartDate = GetOperationalWeekStart(referenceDate);
        var days = new List<WeeklyDoughCalendarDayResponse>(OperationalDays);
        DoughPrepCalculationResult? selectedDayCalculation = null;

        for (var offset = 0; offset < OperationalDays; offset++)
        {
            var day = weekStartDate.AddDays(offset);
            var calculation = await _doughPrepCalculationService.CalculateAsync(
                new CalculateDoughPrepRequest
                {
                    TargetDate = day,
                    HistoricalWeeksToUse = historicalWeeksToUse
                },
                cancellationToken);

            if (day == referenceDate)
            {
                selectedDayCalculation = calculation;
            }

            days.Add(new WeeklyDoughCalendarDayResponse
            {
                Date = day,
                RestaurantDoughBalls = calculation.HistoricalAverageBalls,
                EventDoughBalls = calculation.EventEstimatedBalls,
                TotalNeededBalls = calculation.RequiredBalls,
                AvailableBalls = calculation.AvailableBalls,
                CompletedBalls = calculation.CompletedBalls,
                StillMissingBalls = calculation.MissingBalls,
                Status = DetermineStatus(calculation.RequiredBalls, calculation.AvailableBalls, calculation.CompletedBalls, calculation.MissingBalls)
            });
        }

        selectedDayCalculation ??= await _doughPrepCalculationService.CalculateAsync(
            new CalculateDoughPrepRequest
            {
                TargetDate = referenceDate,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);

        var weekTotalNeededBalls = days.Sum(day => day.TotalNeededBalls);
        var weekCompletedBalls = days.Sum(day => day.CompletedBalls);
        var weekAvailableBalls = Math.Max(selectedDayCalculation.AvailableBalls, 0);
        var weekMissingBalls = Math.Max(
            weekTotalNeededBalls - weekCompletedBalls - weekAvailableBalls,
            0);

        return new WeeklyDoughCalendarResponse
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekStartDate.AddDays(OperationalDays - 1),
            WeekAvailableBalls = weekAvailableBalls,
            WeekTotalNeededBalls = weekTotalNeededBalls,
            WeekCompletedBalls = weekCompletedBalls,
            WeekMissingBalls = weekMissingBalls,
            UpcomingEventBalls = days.Sum(day => day.EventDoughBalls),
            Days = days
        };
    }

    private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private static string DetermineStatus(int totalNeededBalls, int availableBalls, int completedBalls, int stillMissingBalls)
    {
        if (totalNeededBalls <= 0)
        {
            return "No Data";
        }

        if (stillMissingBalls <= 0)
        {
            return "Covered";
        }

        return completedBalls > 0 || availableBalls > 0
            ? "In Progress"
            : "Needs Dough";
    }
}
