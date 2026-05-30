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
        var validationError = await ValidatePayloadAsync(payload);
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var isDocument = _validationFileService.IsDocument(payload.Extension);
            var containerName = isDocument ? "documents" : payload.ContainerName;
            var safeFileName = _validationFileService.SanitizeFileName(payload.OriginalFileName);
            var canonicalContentType = _validationFileService.GetCanonicalContentType(payload.Extension);

            var url = isDocument
                ? await _fileStorage.SaveDocumentAsync(payload.Content, payload.Extension, safeFileName, canonicalContentType, containerName, cancellationToken)
                : await _fileStorage.SaveFileAsync(payload.Content, payload.Extension, canonicalContentType, containerName, cancellationToken);

            return ApiResponse<FileUploadResponse>.Success(
                new FileUploadResponse { Url = url },
                "File uploaded successfully.",
                HttpStatusCode.Created);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error uploading file {FileName}", payload.OriginalFileName);
            return ApiResponse<FileUploadResponse>.Failure(
                "We could not upload the file right now.",
                HttpStatusCode.InternalServerError);
        }
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

        var validationErrors = new List<string>();
        foreach (var file in fileList)
        {
            var validationResponse = await ValidatePayloadAsync(file);
            if (validationResponse is not null)
            {
                validationErrors.Add($"{file.OriginalFileName}: {validationResponse.Message}");
            }
        }

        if (validationErrors.Count > 0)
        {
            return ApiResponse<IEnumerable<FileUploadResponse>>.Failure(
                "One or more files are invalid.",
                HttpStatusCode.BadRequest,
                validationErrors.ToArray());
        }

        var uploads = new List<FileUploadResponse>();

        try
        {
            foreach (var file in fileList)
            {
                var isDocument = _validationFileService.IsDocument(file.Extension);
                var containerName = isDocument ? "documents" : defaultContainerName ?? "images";
                var safeFileName = _validationFileService.SanitizeFileName(file.OriginalFileName);
                var canonicalContentType = _validationFileService.GetCanonicalContentType(file.Extension);

                var url = isDocument
                    ? await _fileStorage.SaveDocumentAsync(file.Content, file.Extension, safeFileName, canonicalContentType, containerName, cancellationToken)
                    : await _fileStorage.SaveFileAsync(file.Content, file.Extension, canonicalContentType, containerName, cancellationToken);

                uploads.Add(new FileUploadResponse { Url = url });
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error uploading multiple files");
            return ApiResponse<IEnumerable<FileUploadResponse>>.Failure(
                "We could not upload the files right now.",
                HttpStatusCode.InternalServerError);
        }

        return ApiResponse<IEnumerable<FileUploadResponse>>.Success(
            uploads,
            "Files uploaded successfully.",
            HttpStatusCode.Created);
    }

    public async Task<ApiResponse<bool>> DeleteFileAsync(
        string filePath,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(containerName))
        {
            return ApiResponse<bool>.Failure(
                "File path and container name are required.",
                HttpStatusCode.BadRequest);
        }

        try
        {
            await _fileStorage.RemoveFileAsync(filePath, containerName, cancellationToken);
            return ApiResponse<bool>.Success(true, "File deleted successfully.", HttpStatusCode.OK);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to delete file {FilePath} from {ContainerName}", filePath, containerName);
            return ApiResponse<bool>.Failure(
                "We could not delete the file right now.",
                HttpStatusCode.InternalServerError);
        }
    }

    public async Task<ApiResponse<FileUploadResponse>> UpdateFileAsync(
        byte[] content,
        string extension,
        string currentFilePath,
        FileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new FileUploadPayload
        {
            Content = content,
            Extension = extension,
            ContentType = request.NewFile.ContentType,
            OriginalFileName = request.NewFile.FileName,
            ContainerName = request.ContainerName
        };

        var validationError = await ValidatePayloadAsync(payload);
        if (validationError is not null)
        {
            return validationError;
        }

        try
        {
            var safeFileName = _validationFileService.SanitizeFileName(request.NewFile.FileName);
            var canonicalContentType = _validationFileService.GetCanonicalContentType(extension);

            var url = await _fileStorage.ReplaceFileAsync(
                content,
                extension,
                canonicalContentType,
                currentFilePath,
                request.ContainerName,
                safeFileName,
                cancellationToken);

            return ApiResponse<FileUploadResponse>.Success(
                new FileUploadResponse { Url = url },
                "File updated successfully.",
                HttpStatusCode.OK);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error updating file {FilePath}", currentFilePath);
            return ApiResponse<FileUploadResponse>.Failure(
                "We could not update the file right now.",
                HttpStatusCode.InternalServerError);
        }
    }

    private async Task<ApiResponse<FileUploadResponse>?> ValidatePayloadAsync(FileUploadPayload payload)
    {
        if (payload.Content.Length == 0)
        {
            return ApiResponse<FileUploadResponse>.Failure("File content is empty.", HttpStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(payload.OriginalFileName))
        {
            return ApiResponse<FileUploadResponse>.Failure("A file name is required.", HttpStatusCode.BadRequest);
        }

        if (string.IsNullOrWhiteSpace(payload.ContentType))
        {
            return ApiResponse<FileUploadResponse>.Failure("A valid content type is required.", HttpStatusCode.BadRequest);
        }

        if (!await _validationFileService.ValidateFileAsync(payload.Content, payload.Extension, payload.ContentType))
        {
            return ApiResponse<FileUploadResponse>.Failure(
                "Invalid file format, content type, or size.",
                HttpStatusCode.BadRequest);
        }

        return null;
    }
}
