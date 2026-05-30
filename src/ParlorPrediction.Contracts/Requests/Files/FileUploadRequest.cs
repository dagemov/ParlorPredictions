using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ParlorPrediction.Contracts.Requests.Files;

public sealed class FileUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(63)]
    public string? ContainerName { get; set; }
}
