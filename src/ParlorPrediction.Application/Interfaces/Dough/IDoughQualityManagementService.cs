using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughQuality;

namespace ParlorPrediction.Application.Interfaces.Dough;

public interface IDoughQualityManagementService
{
    Task<Guid> CreateAsync(SaveDoughBatchQualityRecordRequest request, CancellationToken cancellationToken = default);

    Task<DoughBatchQualityRecordResponse> MarkAsAttentionAsync(
        MarkDoughAsAttentionRequest request,
        CancellationToken cancellationToken = default);

    Task<DoughBatchQualityRecordResponse> CorrectStatusAsync(
        CorrectDoughQualityStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<DoughBatchQualityRecordResponse> DiscardAsync(
        DiscardDoughRequest request,
        CancellationToken cancellationToken = default);

    Task<DoughBatchQualityRecordResponse> ReballAsync(
        ReballDoughRequest request,
        CancellationToken cancellationToken = default);
}
