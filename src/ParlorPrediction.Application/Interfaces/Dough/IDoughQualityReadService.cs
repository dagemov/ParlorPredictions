using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughQuality;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughQualityReadService
{
    Task<IReadOnlyList<DoughBatchQualityRecordResponse>> SearchAsync(
        SearchDoughBatchQualityRecordsRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoughAttentionCandidateResponse>> EvaluateAttentionCandidatesAsync(
        EvaluateDoughAttentionCandidatesRequest request,
        CancellationToken cancellationToken = default);

    Task<DoughQualitySummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<DoughLossAnalyticsResponse> GetLossAnalyticsAsync(
        GetDoughLossAnalyticsRequest request,
        CancellationToken cancellationToken = default);
}
