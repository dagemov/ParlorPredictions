using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Responses.DoughUsage;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughSourceProjectionService : IDoughSourceProjectionService
{
    private readonly IDoughBatchQualityRepository _doughBatchQualityRepository;
    private readonly IDoughUsageTraceRepository _doughUsageTraceRepository;

    public DoughSourceProjectionService(
        IDoughBatchQualityRepository doughBatchQualityRepository,
        IDoughUsageTraceRepository doughUsageTraceRepository)
    {
        _doughBatchQualityRepository = doughBatchQualityRepository;
        _doughUsageTraceRepository = doughUsageTraceRepository;
    }

    public async Task<IReadOnlyList<DoughSourceRemainingResponse>> GetRemainingBySourceAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(referenceDate));
        }

        var records = await _doughBatchQualityRepository.ListAsync(cancellationToken);
        var traces = await _doughUsageTraceRepository.SearchAsync(
            null,
            referenceDate,
            null,
            cancellationToken);

        var usedBySource = traces
            .Where(trace => trace.UsageDate <= referenceDate)
            .GroupBy(trace => trace.SourceDoughBatchQualityRecordId)
            .ToDictionary(group => group.Key, group => group.Sum(trace => trace.BallsUsed));

        return records
            .Where(record => DateOnly.FromDateTime(record.CreatedOrBalledAt.ToLocalTime()) <= referenceDate)
            .Select(record => Map(record, referenceDate, usedBySource.GetValueOrDefault(record.Id)))
            .OrderBy(item => item.SourceDate)
            .ThenBy(item => item.CreatedOrBalledAt)
            .ToArray();
    }

    private static DoughSourceRemainingResponse Map(
        DoughBatchQualityRecord record,
        DateOnly referenceDate,
        int usedBalls)
    {
        var ageDays = DoughQualityRules.CalculateOperationalAgeDays(record.CreatedOrBalledAt.ToLocalTime(), referenceDate);
        var remainingBalls = record.CountsAsAvailable
            ? Math.Max(record.QuantityBalls - usedBalls, 0)
            : 0;
        var recommendation = DetermineRecommendation(record, referenceDate, ageDays, remainingBalls);

        return new DoughSourceRemainingResponse
        {
            SourceDoughBatchQualityRecordId = record.Id,
            SourceDate = record.SourceDate,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            SourceType = record.CurrentStatus.ToString(),
            MustUseByDate = record.MustUseByDate,
            AgeDays = ageDays,
            OriginalBalls = record.QuantityBalls,
            UsedBalls = usedBalls,
            RemainingBalls = remainingBalls,
            CountsAsAvailable = record.CountsAsAvailable,
            IsReballCandidate = recommendation == DoughActionRecommendation.Reball,
            IsDiscardCandidate = recommendation == DoughActionRecommendation.Discard,
            RecommendedAction = recommendation.ToString()
        };
    }

    private static DoughActionRecommendation DetermineRecommendation(
        DoughBatchQualityRecord record,
        DateOnly referenceDate,
        int ageDays,
        int remainingBalls)
    {
        if (!record.CountsAsAvailable || remainingBalls <= 0)
        {
            return DoughActionRecommendation.None;
        }

        if (record.CurrentStatus == DoughQualityStatus.MustUseNextDay)
        {
            return record.MustUseByDate.HasValue && referenceDate > record.MustUseByDate.Value
                ? DoughActionRecommendation.Discard
                : DoughActionRecommendation.UseFirst;
        }

        if (record.CurrentStatus == DoughQualityStatus.Reballed)
        {
            return DoughActionRecommendation.UseFirst;
        }

        if (ageDays > DoughQualityRules.AttentionCandidatePreferredMaximumDays + 2)
        {
            return DoughActionRecommendation.Discard;
        }

        if (ageDays > DoughQualityRules.AttentionCandidatePreferredMaximumDays)
        {
            return DoughActionRecommendation.Reball;
        }

        if (record.CurrentStatus == DoughQualityStatus.Attention ||
            ageDays >= DoughQualityRules.AttentionCandidateMinimumDays)
        {
            return DoughActionRecommendation.Review;
        }

        return DoughActionRecommendation.None;
    }
}
