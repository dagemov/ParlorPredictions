using ParlorPrediction.Application.Models.Files;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Files;
using ParlorPrediction.Contracts.Responses.Files;

namespace ParlorPrediction.Application.Interfaces.Files;

public interface IFileBlobService
{
    Task<ApiResponse<FileUploadResponse>> UploadFileAsync(
        FileUploadPayload payload,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IEnumerable<FileUploadResponse>>> UploadMultipleFilesAsync(
        IEnumerable<FileUploadPayload> files,
        string? defaultContainerName,
        int maxFiles = 4,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(
        string filePath,
        string containerName,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<FileUploadResponse>> UpdateFileAsync(
        byte[] content,
        string extension,
        string currentFilePath,
        FileUpdateRequest request,
        CancellationToken cancellationToken = default);
}
