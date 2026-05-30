using System.Net;
using Microsoft.Extensions.Logging;
using ParlorPrediction.Application.Interfaces.Files;
using ParlorPrediction.Application.Models.Files;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Files;
using ParlorPrediction.Contracts.Responses.Files;

namespace ParlorPrediction.Application.Services.Files;

public sealed class FileBlobService : IFileBlobService
{
    private readonly IFileStorage _fileStorage;
    private readonly IValidationFileService _validationFileService;
    private readonly ILogger<FileBlobService> _logger;

    public FileBlobService(
        IFileStorage fileStorage,
        IValidationFileService validationFileService,
        ILogger<FileBlobService> logger)
    {
        _fileStorage = fileStorage;
        _validationFileService = validationFileService;
        _logger = logger;
    }

    public async Task<ApiResponse<FileUploadResponse>> UploadFileAsync(
        FileUploadPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload.Content.Length == 0)
        {
            return ApiResponse<FileUploadResponse>.Failure("File content is empty.", HttpStatusCode.BadRequest);
        }

        if (!await _validationFileService.ValidateFileAsync(payload.Content, payload.Extension))
        {
            return ApiResponse<FileUploadResponse>.Failure("Invalid file format or size.", HttpStatusCode.BadRequest);
        }

        var isDocument = _validationFileService.IsDocument(payload.Extension);
        var containerName = isDocument ? "documents" : payload.ContainerName;

        var url = isDocument
            ? await _fileStorage.SaveDocumentAsync(payload.Content, payload.Extension, payload.OriginalFileName, containerName, cancellationToken)
            : await _fileStorage.SaveFileAsync(payload.Content, payload.Extension, containerName, cancellationToken);

        return ApiResponse<FileUploadResponse>.Success(
            new FileUploadResponse { Url = url },
            "File uploaded successfully.",
            HttpStatusCode.Created);
    }

    public async Task<ApiResponse<IEnumerable<FileUploadResponse>>> UploadMultipleFilesAsync(
        IEnumerable<FileUploadPayload> files,
        string? defaultContainerName,
        int maxFiles = 4,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        if (fileList.Count > maxFiles)
        {
            return ApiResponse<IEnumerable<FileUploadResponse>>.Failure(
                $"Max files allowed: {maxFiles}.",
                HttpStatusCode.BadRequest);
        }

        var uploads = new List<FileUploadResponse>();

        foreach (var file in fileList)
        {
            if (!await _validationFileService.ValidateFileAsync(file.Content, file.Extension))
            {
                continue;
            }

            var isDocument = _validationFileService.IsDocument(file.Extension);
            var containerName = isDocument ? "documents" : defaultContainerName ?? "images";

            var url = isDocument
                ? await _fileStorage.SaveDocumentAsync(file.Content, file.Extension, file.OriginalFileName, containerName, cancellationToken)
                : await _fileStorage.SaveFileAsync(file.Content, file.Extension, containerName, cancellationToken);

            uploads.Add(new FileUploadResponse { Url = url });
        }

        return ApiResponse<IEnumerable<FileUploadResponse>>.Success(
            uploads,
            "Files uploaded successfully.",
            HttpStatusCode.Created);
    }

    public async Task DeleteFileAsync(
        string filePath,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(containerName))
        {
            return;
        }

        try
        {
            await _fileStorage.RemoveFileAsync(filePath, containerName, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to delete file {FilePath} from {ContainerName}", filePath, containerName);
        }
    }

    public async Task<ApiResponse<FileUploadResponse>> UpdateFileAsync(
        byte[] content,
        string extension,
        string currentFilePath,
        FileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _validationFileService.ValidateFileAsync(content, extension))
        {
            return ApiResponse<FileUploadResponse>.Failure("New file is invalid.", HttpStatusCode.BadRequest);
        }

        var url = await _fileStorage.ReplaceFileAsync(
            content,
            extension,
            currentFilePath,
            request.ContainerName,
            request.NewFile.FileName,
            cancellationToken);

        return ApiResponse<FileUploadResponse>.Success(
            new FileUploadResponse { Url = url },
            "File updated successfully.",
            HttpStatusCode.OK);
    }
}
