using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class PrepCatalogReadService : IPrepCatalogReadService
{
    private readonly IPrepItemReadRepository _prepItemReadRepository;
    private readonly IPrepStationReadRepository _prepStationReadRepository;

    public PrepCatalogReadService(
        IPrepItemReadRepository prepItemReadRepository,
        IPrepStationReadRepository prepStationReadRepository)
    {
        _prepItemReadRepository = prepItemReadRepository;
        _prepStationReadRepository = prepStationReadRepository;
    }

    public async Task<PrepCatalogOptionsResponse> GetActiveOptionsAsync(CancellationToken cancellationToken = default)
    {
        var prepItems = await _prepItemReadRepository.GetActiveAsync(cancellationToken);
        var prepStations = await _prepStationReadRepository.GetActiveAsync(cancellationToken);

        return new PrepCatalogOptionsResponse
        {
            PrepItems = prepItems
                .Select(item => new PrepItemOptionResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    Code = item.Code,
                    PrepStationId = item.PrepStationId,
                    PrepStationName = item.PrepStation.Name
                })
                .ToArray(),
            PrepStations = prepStations
                .Select(station => new PrepStationOptionResponse
                {
                    Id = station.Id,
                    Name = station.Name,
                    Code = station.Code
                })
                .ToArray()
        };
    }
}
