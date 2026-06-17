using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class WeeklyDoughClosingReadService : IWeeklyDoughClosingReadService
{
    private readonly IWeeklyDoughClosingRepository _weeklyDoughClosingRepository;

    public WeeklyDoughClosingReadService(IWeeklyDoughClosingRepository weeklyDoughClosingRepository)
    {
        _weeklyDoughClosingRepository = weeklyDoughClosingRepository;
    }

    public async Task<IReadOnlyList<WeeklyDoughClosingResponse>> GetWeeklyClosingsAsync(
        GetWeeklyClosingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = await _weeklyDoughClosingRepository.ListAsync(
            request.FromWeekStartDate,
            request.ToWeekStartDate,
            cancellationToken);

        return items
            .Select(Map)
            .ToArray();
    }

    public async Task<WeeklyDoughCarryoverResponse> GetCarryoverForWeekAsync(
        GetWeeklyDoughCarryoverRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.WeekStartDate == default)
        {
            throw new ArgumentException("Week start date is required.", nameof(request.WeekStartDate));
        }

        var targetWeekStartDate = NormalizeClosingWeekStart(request.WeekStartDate);
        var targetWeekEndDate = targetWeekStartDate.AddDays(WeeklyDoughClosing.ClosingWeekLengthDays - 1);
        var previousWeekStartDate = targetWeekStartDate.AddDays(-7);
        var sourceClosing = await FindClosingForWeekAsync(previousWeekStartDate, cancellationToken);

        if (sourceClosing is null)
        {
            return new WeeklyDoughCarryoverResponse
            {
                TargetWeekStartDate = targetWeekStartDate,
                TargetWeekEndDate = targetWeekEndDate
            };
        }

        return new WeeklyDoughCarryoverResponse
        {
            TargetWeekStartDate = targetWeekStartDate,
            TargetWeekEndDate = targetWeekEndDate,
            HasClosingCarryover = true,
            SourceWeekStartDate = sourceClosing.WeekStartDate,
            SourceWeekEndDate = sourceClosing.WeekEndDate,
            CarryoverReadyBalls = sourceClosing.LeftoverReadyBalls,
            CarryoverAttentionBalls = sourceClosing.LeftoverAttentionBalls,
            CarryoverAvailableBalls = sourceClosing.CarryoverAvailableBalls,
            MixedButNotBalledLoads = sourceClosing.LeftoverMixedLoads,
            PreviousWeekProducedBalls = sourceClosing.ProducedBalls,
            PreviousWeekUsedBalls = sourceClosing.UsedBalls,
            PreviousWeekLostBalls = sourceClosing.LostBalls,
            ClosingNotes = sourceClosing.Notes
        };
    }

    private static DateOnly NormalizeClosingWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private async Task<WeeklyDoughClosing?> FindClosingForWeekAsync(
        DateOnly normalizedWeekStartDate,
        CancellationToken cancellationToken)
    {
        var exactMatch = await _weeklyDoughClosingRepository.GetByWeekStartDateAsync(normalizedWeekStartDate, cancellationToken);
        if (exactMatch is not null)
        {
            return exactMatch;
        }

        var legacyTuesdayWeekStartDate = normalizedWeekStartDate.AddDays(1);
        return await _weeklyDoughClosingRepository.GetByWeekStartDateAsync(legacyTuesdayWeekStartDate, cancellationToken);
    }

    private static WeeklyDoughClosingResponse Map(WeeklyDoughClosing closing)
    {
        return new WeeklyDoughClosingResponse
        {
            Id = closing.Id,
            WeekStartDate = closing.WeekStartDate,
            WeekEndDate = closing.WeekEndDate,
            NeededBalls = closing.NeededBalls,
            ProducedBalls = closing.ProducedBalls,
            UsedBalls = closing.UsedBalls,
            LostBalls = closing.LostBalls,
            LeftoverReadyBalls = closing.LeftoverReadyBalls,
            LeftoverAttentionBalls = closing.LeftoverAttentionBalls,
            LeftoverMixedLoads = closing.LeftoverMixedLoads,
            CarryoverAvailableBalls = closing.CarryoverAvailableBalls,
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
