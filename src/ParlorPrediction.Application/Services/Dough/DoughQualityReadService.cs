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
    private readonly IDoughSourceProjectionService _doughSourceProjectionService;
    private readonly IDoughLossRecordRepository _doughLossRecordRepository;

    public DoughQualityReadService(
        IDoughBatchQualityRepository doughBatchQualityRepository,
        IDoughSourceProjectionService doughSourceProjectionService,
        IDoughLossRecordRepository doughLossRecordRepository)
    {
        _doughBatchQualityRepository = doughBatchQualityRepository;
        _doughSourceProjectionService = doughSourceProjectionService;
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
        var remainingBySource = await _doughSourceProjectionService.GetRemainingBySourceAsync(
            request.ReferenceDate,
            cancellationToken);
        var remainingLookup = remainingBySource.ToDictionary(item => item.SourceDoughBatchQualityRecordId);

        return records
            .Where(record => remainingLookup.TryGetValue(record.Id, out var source) &&
                source.RemainingBalls > 0 &&
                (string.Equals(source.RecommendedAction, DoughActionRecommendation.Review.ToString(), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(source.RecommendedAction, DoughActionRecommendation.Reball.ToString(), StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(source.RecommendedAction, DoughActionRecommendation.Discard.ToString(), StringComparison.OrdinalIgnoreCase)))
            .OrderBy(record => record.SourceDate)
            .ThenBy(record => record.CreatedOrBalledAt)
            .Select(record => MapCandidate(record, remainingLookup[record.Id], request.ReferenceDate))
            .ToArray();
    }

    public async Task<DoughQualitySummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var records = await _doughBatchQualityRepository.ListAsync(cancellationToken);
        var referenceDate = DateOnly.FromDateTime(DateTime.Today);
        var remainingBySource = await _doughSourceProjectionService.GetRemainingBySourceAsync(referenceDate, cancellationToken);

        return new DoughQualitySummaryResponse
        {
            GoodBalls = SumRemainingByStatus(remainingBySource, DoughQualityStatus.Good),
            AttentionBalls = SumRemainingByStatus(remainingBySource, DoughQualityStatus.Attention),
            ReballedBalls = SumRemainingByStatus(remainingBySource, DoughQualityStatus.Reballed),
            MustUseNextDayBalls = SumRemainingByStatus(remainingBySource, DoughQualityStatus.MustUseNextDay),
            DiscardedBalls = records.Where(record => record.CurrentStatus == DoughQualityStatus.Discarded).Sum(record => record.QuantityBalls),
            TotalAvailableBalls = remainingBySource.Where(record => record.CountsAsAvailable).Sum(record => record.RemainingBalls)
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
        Contracts.Responses.DoughUsage.DoughSourceRemainingResponse source,
        DateOnly referenceDate)
    {
        var ageDays = DoughQualityRules.CalculateOperationalAgeDays(record.CreatedOrBalledAt, referenceDate);
        var candidateReason = source.RecommendedAction switch
        {
            nameof(DoughActionRecommendation.Discard) => "Remaining dough is now past the safe use window and needs a manager discard decision.",
            nameof(DoughActionRecommendation.Reball) => "Remaining dough has moved past the preferred attention window and should be reviewed for reball.",
            _ => "Remaining dough has reached the attention window and needs review."
        };

        return new DoughAttentionCandidateResponse
        {
            DoughBatchQualityRecordId = record.Id,
            SourceDate = record.SourceDate,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            QuantityBalls = source.RemainingBalls,
            CurrentStatus = record.CurrentStatus.ToString(),
            AgeDays = ageDays,
            CandidateReason = candidateReason
        };
    }

    private static int SumRemainingByStatus(
        IReadOnlyList<Contracts.Responses.DoughUsage.DoughSourceRemainingResponse> remainingBySource,
        DoughQualityStatus status)
    {
        return remainingBySource
            .Where(record => string.Equals(record.SourceType, status.ToString(), StringComparison.OrdinalIgnoreCase))
            .Sum(record => record.RemainingBalls);
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
