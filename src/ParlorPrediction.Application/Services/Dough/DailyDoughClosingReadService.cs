using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DailyDoughClosingReadService : IDailyDoughClosingReadService
{
    private const int OperationalDays = WeeklyDoughClosing.OperationalWeekLengthDays;

    private readonly IDailyDoughClosingRepository _dailyDoughClosingRepository;
    private readonly IDoughPrepCalculationService _doughPrepCalculationService;
    private readonly IDoughUsageTraceRepository _doughUsageTraceRepository;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;

    public DailyDoughClosingReadService(
        IDailyDoughClosingRepository dailyDoughClosingRepository,
        IDoughPrepCalculationService doughPrepCalculationService,
        IDoughUsageTraceRepository doughUsageTraceRepository,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService)
    {
        _dailyDoughClosingRepository = dailyDoughClosingRepository;
        _doughPrepCalculationService = doughPrepCalculationService;
        _doughUsageTraceRepository = doughUsageTraceRepository;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
    }

    public async Task<DailyDoughClosingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var closing = await _dailyDoughClosingRepository.GetByIdAsync(id, cancellationToken);
        return closing is null ? null : Map(closing);
    }

    public async Task<DailyClosingWeekSummaryResponse> GetWeekSummaryAsync(
        GetDailyClosingWeekSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReferenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(request.ReferenceDate));
        }

        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1 ? 8 : request.HistoricalWeeksToUse;
        var weekStartDate = GetOperationalWeekStart(request.ReferenceDate);
        var weekEndDate = weekStartDate.AddDays(OperationalDays - 1);
        var closings = await _dailyDoughClosingRepository.ListByWeekStartDateAsync(weekStartDate, cancellationToken);
        var closingByDate = closings.ToDictionary(closing => closing.ClosingDate);
        var days = new List<DailyClosingWeekDayResponse>(OperationalDays);

        for (var offset = 0; offset < OperationalDays; offset++)
        {
            var day = weekStartDate.AddDays(offset);
            var forecast = await GetForecastForDayAsync(day, historicalWeeksToUse, cancellationToken);
            closingByDate.TryGetValue(day, out var closing);

            days.Add(new DailyClosingWeekDayResponse
            {
                Date = day,
                ForecastNeededBalls = closing?.ForecastNeededBalls ?? forecast,
                ActualUsedBalls = closing?.ActualUsedBalls,
                DailyVariance = closing?.DailyVariance,
                IsClosed = closing is not null,
                DailyClosingId = closing?.Id,
                Notes = closing?.Notes,
                IsToday = day == request.ReferenceDate,
                IsFuture = day > request.ReferenceDate
            });
        }

        var closedDays = days.Where(day => day.IsClosed).ToArray();
        var accumulatedVariance = closedDays.Sum(day => day.DailyVariance ?? 0);
        var accumulatedSurplus = closedDays.Where(day => day.DailyVariance > 0).Sum(day => day.DailyVariance ?? 0);
        var accumulatedShortage = closedDays.Where(day => day.DailyVariance < 0).Sum(day => Math.Abs(day.DailyVariance ?? 0));

        return new DailyClosingWeekSummaryResponse
        {
            ReferenceDate = request.ReferenceDate,
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            Days = days,
            TotalForecastBalls = days.Sum(day => day.ForecastNeededBalls),
            TotalActualUsedBalls = closedDays.Sum(day => day.ActualUsedBalls ?? 0),
            AccumulatedVariance = accumulatedVariance,
            AccumulatedSurplus = accumulatedSurplus,
            AccumulatedShortage = accumulatedShortage,
            ClosedDaysCount = closedDays.Length
        };
    }

    public async Task<DailyClosingOperationalInsightsResponse> GetOperationalInsightsAsync(
        GetDailyClosingWeekSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var historicalWeeksToUse = request.HistoricalWeeksToUse < 1 ? 8 : request.HistoricalWeeksToUse;
        var weekSummary = await GetWeekSummaryAsync(request, cancellationToken);
        var weeklyCalendar = await _prepWeeklyDoughCalendarService.GetWeekAsync(
            request.ReferenceDate,
            historicalWeeksToUse,
            cancellationToken);
        var traces = await _doughUsageTraceRepository.SearchAsync(
            weekSummary.WeekStartDate,
            request.ReferenceDate,
            null,
            cancellationToken);

        var remainingForecastNeed = weekSummary.Days
            .Where(day => day.Date >= request.ReferenceDate)
            .Sum(day => day.ForecastNeededBalls);

        var adjustedRemainingForecastNeed = Math.Max(
            remainingForecastNeed - weekSummary.AccumulatedSurplus,
            0);

        var currentAvailable = weeklyCalendar.ReadyNowBalls;
        var stillFermenting = weeklyCalendar.StillFermentingBalls;
        var mixedNotBalled = weeklyCalendar.MixedButNotBalledBalls;
        var projectedSurplus = currentAvailable + stillFermenting + mixedNotBalled - adjustedRemainingForecastNeed;
        var tracedUsedBallsOnClosedDays = SumTraceBallsOnClosedDays(weekSummary, traces);
        var traceReconciliationDifference = weekSummary.TotalActualUsedBalls - tracedUsedBallsOnClosedDays;
        var hasTraceReconciliationWarning = weekSummary.ClosedDaysCount > 0 && traceReconciliationDifference != 0;

        return new DailyClosingOperationalInsightsResponse
        {
            ReferenceDate = request.ReferenceDate,
            WeekStartDate = weekSummary.WeekStartDate,
            WeekEndDate = weekSummary.WeekEndDate,
            AccumulatedVariance = weekSummary.AccumulatedVariance,
            AccumulatedSurplus = weekSummary.AccumulatedSurplus,
            AccumulatedShortage = weekSummary.AccumulatedShortage,
            TotalActualUsedBalls = weekSummary.TotalActualUsedBalls,
            ClosedDaysCount = weekSummary.ClosedDaysCount,
            CurrentAvailableBalls = currentAvailable,
            StillFermentingBalls = stillFermenting,
            MixedButNotBalledBalls = mixedNotBalled,
            RemainingForecastNeed = remainingForecastNeed,
            AdjustedRemainingForecastNeed = adjustedRemainingForecastNeed,
            DailyClosingVarianceApplied = weekSummary.AccumulatedSurplus,
            ProjectedSurplus = projectedSurplus,
            HasSurplusWarning = projectedSurplus > 0,
            HasShortageWarning = projectedSurplus < 0,
            TotalTracedUsedBallsOnClosedDays = tracedUsedBallsOnClosedDays,
            TraceReconciliationDifferenceBalls = traceReconciliationDifference,
            HasTraceReconciliationWarning = hasTraceReconciliationWarning,
            TraceReconciliationMessage = hasTraceReconciliationWarning
                ? BuildTraceReconciliationMessage(
                    weekSummary.TotalActualUsedBalls,
                    tracedUsedBallsOnClosedDays,
                    traceReconciliationDifference)
                : null,
            Recommendation = BuildRecommendation(projectedSurplus, weekSummary.AccumulatedVariance)
        };
    }

    private async Task<int> GetForecastForDayAsync(
        DateOnly day,
        int historicalWeeksToUse,
        CancellationToken cancellationToken)
    {
        var calculation = await _doughPrepCalculationService.CalculateAsync(
            new CalculateDoughPrepRequest
            {
                TargetDate = day,
                HistoricalWeeksToUse = historicalWeeksToUse
            },
            cancellationToken);

        return calculation.RequiredBalls;
    }

    private static string BuildRecommendation(int projectedSurplus, int accumulatedVariance)
    {
        if (projectedSurplus > 0 && accumulatedVariance > 0)
        {
            return "Consider reducing upcoming dough production. Daily usage is below forecast and projected dough exceeds remaining need.";
        }

        if (projectedSurplus > 0)
        {
            return "Consider reducing upcoming dough production.";
        }

        if (projectedSurplus < 0)
        {
            return "Additional loads required.";
        }

        if (accumulatedVariance > 0)
        {
            return "Usage is running below forecast this week. Monitor accumulation before adding more loads.";
        }

        if (accumulatedVariance < 0)
        {
            return "Usage is running above forecast this week. Watch available dough closely.";
        }

        return "Production pace looks aligned with forecast.";
    }

    private static int SumTraceBallsOnClosedDays(
        DailyClosingWeekSummaryResponse weekSummary,
        IReadOnlyList<DoughUsageTrace> traces)
    {
        var closedDates = weekSummary.Days
            .Where(day => day.IsClosed)
            .Select(day => day.Date)
            .ToHashSet();

        return traces
            .Where(trace => closedDates.Contains(trace.UsageDate))
            .Sum(trace => trace.BallsUsed);
    }

    private static string BuildTraceReconciliationMessage(
        int actualUsedBalls,
        int tracedUsedBalls,
        int traceReconciliationDifference)
    {
        var absoluteDifference = Math.Abs(traceReconciliationDifference);

        if (traceReconciliationDifference > 0)
        {
            return $"Daily Closing shows {actualUsedBalls} balls used on closed days, but usage traces only explain {tracedUsedBalls}. Add or correct {absoluteDifference} traced balls so source tracking matches the closing total.";
        }

        return $"Usage traces explain {tracedUsedBalls} balls on closed days, but Daily Closing only shows {actualUsedBalls}. Remove or correct {absoluteDifference} traced balls, or review the closing totals.";
    }

    private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private static DailyDoughClosingResponse Map(DailyDoughClosing closing)
    {
        return new DailyDoughClosingResponse
        {
            Id = closing.Id,
            ClosingDate = closing.ClosingDate,
            WeekStartDate = closing.WeekStartDate,
            ForecastNeededBalls = closing.ForecastNeededBalls,
            ActualUsedBalls = closing.ActualUsedBalls,
            DailyVariance = closing.DailyVariance,
            Notes = closing.Notes,
            ClosedByUserId = closing.ClosedByUserId,
            ClosedAtUtc = closing.ClosedAtUtc,
            WasCorrected = closing.WasCorrected,
            CorrectedByUserId = closing.CorrectedByUserId,
            CorrectedAtUtc = closing.CorrectedAtUtc,
            CorrectionNote = closing.CorrectionNote
        };
    }
}
