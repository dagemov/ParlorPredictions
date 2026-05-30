using Microsoft.AspNetCore.Http;

namespace ParlorPrediction.Contracts.Requests.Files;

public sealed class MultipleFileUploadRequest
{
    public IEnumerable<IFormFile> Files { get; set; } = [];

    public string? ContainerName { get; set; }

    public int MaxFiles { get; set; } = 4;
}
