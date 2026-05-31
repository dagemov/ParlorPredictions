using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Interfaces.Prep;

public interface IPrepCatalogReadService
{
    Task<PrepCatalogOptionsResponse> GetActiveOptionsAsync(CancellationToken cancellationToken = default);
}
