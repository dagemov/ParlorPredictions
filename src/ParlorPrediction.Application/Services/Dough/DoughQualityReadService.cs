using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughQuality;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughQualityReadService : IDoughQualityReadService
{
    private readonly IDoughBatchQualityRepository _doughBatchQualityRepository;
    private readonly IDoughLossRecordRepository _doughLossRecordRepository;

    public DoughQualityReadService(
        IDoughBatchQualityRepository doughBatchQualityRepository,
        IDoughLossRecordRepository doughLossRecordRepository)
    {
        _doughBatchQualityRepository = doughBatchQualityRepository;
        _doughLossRecordRepository = doughLossRecordRepository;
    }

    public async Task<IReadOnlyList<DoughBatchQualityRecordResponse>> SearchAsync(
        SearchDoughBatchQualityRecordsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var status = ParseOptionalStatus(request.CurrentStatus);
        var records = await _doughBatchQualityRepository.SearchAsync(
            request.SourceDateFrom,
            request.SourceDateTo,
            request.CreatedOrBalledFromDate,
            request.CreatedOrBalledToDate,
            request.ReballedFromDate,
            request.ReballedToDate,
            status,
            cancellationToken);

        return records.Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<DoughAttentionCandidateResponse>> EvaluateAttentionCandidatesAsync(
        EvaluateDoughAttentionCandidatesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReferenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(request));
        }

        var records = await _doughBatchQualityRepository.ListAsync(cancellationToken);

        return records
            .Where(record => DoughQualityRules.IsAttentionCandidate(
                record.CurrentStatus,
                record.CreatedOrBalledAt,
                request.ReferenceDate,
                record.MustUseByDate))
            .OrderBy(record => record.SourceDate)
            .ThenBy(record => record.CreatedOrBalledAt)
            .Select(record => MapCandidate(record, request.ReferenceDate))
            .ToArray();
    }

    public async Task<DoughQualitySummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var records = await _doughBatchQualityRepository.ListAsync(cancellationToken);

        return new DoughQualitySummaryResponse
        {
            GoodBalls = records.Where(record => record.CurrentStatus == DoughQualityStatus.Good).Sum(record => record.QuantityBalls),
            AttentionBalls = records.Where(record => record.CurrentStatus == DoughQualityStatus.Attention).Sum(record => record.QuantityBalls),
            ReballedBalls = records.Where(record => record.CurrentStatus == DoughQualityStatus.Reballed).Sum(record => record.QuantityBalls),
            MustUseNextDayBalls = records.Where(record => record.CurrentStatus == DoughQualityStatus.MustUseNextDay).Sum(record => record.QuantityBalls),
            DiscardedBalls = records.Where(record => record.CurrentStatus == DoughQualityStatus.Discarded).Sum(record => record.QuantityBalls),
            TotalAvailableBalls = records.Where(record => record.CountsAsAvailable).Sum(record => record.QuantityBalls)
        };
    }

    public async Task<DoughLossAnalyticsResponse> GetLossAnalyticsAsync(
        GetDoughLossAnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lossReason = ParseOptionalLossReason(request.LossReason);
        var records = await _doughLossRecordRepository.SearchAsync(
            request.FromDate,
            request.ToDate,
            lossReason,
            cancellationToken);

        var items = records
            .GroupBy(record => new { record.LossDate, record.LossReason })
            .OrderBy(group => group.Key.LossDate)
            .ThenBy(group => group.Key.LossReason)
            .Select(group => new DoughLossAnalyticsItemResponse
            {
                LossDate = group.Key.LossDate,
                LossReason = group.Key.LossReason.ToString(),
                QuantityLostBalls = group.Sum(item => item.QuantityLostBalls)
            })
            .ToArray();

        return new DoughLossAnalyticsResponse
        {
            TotalLostBalls = items.Sum(item => item.QuantityLostBalls),
            Items = items
        };
    }

    private static DoughBatchQualityRecordResponse Map(DoughBatchQualityRecord record)
    {
        return new DoughBatchQualityRecordResponse
        {
            Id = record.Id,
            SourceDate = record.SourceDate,
            OriginalDoughTaskId = record.OriginalDoughTaskId,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            QuantityBalls = record.QuantityBalls,
            CurrentStatus = record.CurrentStatus.ToString(),
            StatusReason = record.StatusReason,
            AttentionMarkedAt = record.AttentionMarkedAt,
            ReballedAt = record.ReballedAt,
            MustUseByDate = record.MustUseByDate,
            DiscardedAt = record.DiscardedAt,
            DiscardReason = record.DiscardReason?.ToString(),
            ManagerNote = record.ManagerNote,
            CreatedByUserId = record.CreatedByUserId,
            UpdatedByUserId = record.UpdatedByUserId,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
            CountsAsAvailable = record.CountsAsAvailable
        };
    }

    private static DoughAttentionCandidateResponse MapCandidate(
        DoughBatchQualityRecord record,
        DateOnly referenceDate)
    {
        var ageDays = DoughQualityRules.CalculateOperationalAgeDays(record.CreatedOrBalledAt, referenceDate);
        var candidateReason = record.CurrentStatus == DoughQualityStatus.MustUseNextDay &&
            record.MustUseByDate.HasValue &&
            referenceDate > record.MustUseByDate.Value
            ? "MustUseNextDay deadline has passed."
            : ageDays <= DoughQualityRules.AttentionCandidatePreferredMaximumDays
                ? "Dough has reached the attention age window."
                : "Dough has exceeded the preferred attention age window.";

        return new DoughAttentionCandidateResponse
        {
            DoughBatchQualityRecordId = record.Id,
            SourceDate = record.SourceDate,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            QuantityBalls = record.QuantityBalls,
            CurrentStatus = record.CurrentStatus.ToString(),
            AgeDays = ageDays,
            CandidateReason = candidateReason
        };
    }

    private static DoughQualityStatus? ParseOptionalStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DoughQualityStatusExtensions.TryParse(value, out var status))
        {
            throw new ArgumentException("The dough quality status is not valid.", nameof(value));
        }

        return status;
    }

    private static DoughLossReason? ParseOptionalLossReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DoughLossReasonExtensions.TryParse(value, out var reason))
        {
            throw new ArgumentException("The dough loss reason is not valid.", nameof(value));
        }

        return reason;
    }
}
