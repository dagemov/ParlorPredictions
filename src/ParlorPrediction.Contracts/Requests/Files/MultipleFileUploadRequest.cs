using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Files;

public sealed class MultipleFileUploadRequest
{
    public IEnumerable<IFormFile> Files { get; set; } = [];

    [StringLength(63)]
    public string? ContainerName { get; set; }

    [Range(1, 10)]
    public int MaxFiles { get; set; } = 4;
}
