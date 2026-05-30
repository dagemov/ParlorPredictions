using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Files;
using ParlorPrediction.Application.Models.Files;
using ParlorPrediction.Contracts.Requests.Files;

namespace ParlorPrediction.Mvc.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class FilesController : ControllerBase
{
    private readonly IFileBlobService _fileBlobService;

    public FilesController(IFileBlobService fileBlobService)
    {
        _fileBlobService = fileBlobService;
    }

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] FileUploadRequest request, CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var payload = new FileUploadPayload
        {
            Content = memoryStream.ToArray(),
            Extension = Path.GetExtension(request.File.FileName),
            OriginalFileName = request.File.FileName,
            ContainerName = request.ContainerName ?? "images"
        };

        var response = await _fileBlobService.UploadFileAsync(payload, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("multiple")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadMultiple([FromForm] MultipleFileUploadRequest request, CancellationToken cancellationToken)
    {
        if (!request.Files.Any())
        {
            return BadRequest("No files were provided.");
        }

        var payloads = new List<FileUploadPayload>();

        foreach (var file in request.Files)
        {
            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            payloads.Add(new FileUploadPayload
            {
                Content = memoryStream.ToArray(),
                Extension = Path.GetExtension(file.FileName),
                OriginalFileName = file.FileName,
                ContainerName = request.ContainerName ?? "images"
            });
        }

        var response = await _fileBlobService.UploadMultipleFilesAsync(
            payloads,
            request.ContainerName,
            request.MaxFiles,
            cancellationToken);

        return StatusCode((int)response.StatusCode, response);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string filePath, [FromQuery] string containerName, CancellationToken cancellationToken)
    {
        await _fileBlobService.DeleteFileAsync(filePath, containerName, cancellationToken);
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromForm] FileUpdateRequest request, CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await request.NewFile.CopyToAsync(memoryStream, cancellationToken);

        var response = await _fileBlobService.UpdateFileAsync(
            memoryStream.ToArray(),
            Path.GetExtension(request.NewFile.FileName),
            request.CurrentFilePath,
            request,
            cancellationToken);

        return StatusCode((int)response.StatusCode, response);
    }
}
