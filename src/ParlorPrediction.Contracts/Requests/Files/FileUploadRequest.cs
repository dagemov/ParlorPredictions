using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ParlorPrediction.Contracts.Requests.Files;

public sealed class FileUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public string? ContainerName { get; set; }
}
