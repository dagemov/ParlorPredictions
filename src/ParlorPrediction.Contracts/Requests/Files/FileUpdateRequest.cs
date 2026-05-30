using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ParlorPrediction.Contracts.Requests.Files;

public sealed class FileUpdateRequest
{
    [Required]
    public IFormFile NewFile { get; set; } = null!;

    [Required]
    public string CurrentFilePath { get; set; } = null!;

    [Required]
    [StringLength(63)]
    public string ContainerName { get; set; } = null!;
}
