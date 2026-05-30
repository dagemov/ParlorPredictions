using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Files;
using ParlorPrediction.Application.Models.Files;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Files;
using System.Net;

namespace ParlorPrediction.Mvc.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileBlobService _fileBlobService;

    public FilesController(IFileBlobService fileBlobService)
    {
        _fileBlobService = fileBlobService;
    }

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] FileUploadRequest request, CancellationToken cancellationToken)
    {
        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var payload = new FileUploadPayload
        {
            Content = memoryStream.ToArray(),
            Extension = Path.GetExtension(request.File.FileName),
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            ContainerName = request.ContainerName ?? "images"
        };

        var response = await _fileBlobService.UploadFileAsync(payload, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("multiple")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMultiple([FromForm] MultipleFileUploadRequest request, CancellationToken cancellationToken)
    {
        if (!request.Files.Any())
        {
            return BadRequest(ApiResponse<object>.Failure("No files were provided.", HttpStatusCode.BadRequest));
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
                ContentType = file.ContentType,
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
        var response = await _fileBlobService.DeleteFileAsync(filePath, containerName, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPut]
    [Consumes("multipart/form-data")]
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
