namespace ParlorPrediction.Application.Interfaces.Files;

public interface IFileStorage
{
    Task<string> SaveFileAsync(
        byte[] content,
        string extension,
        string contentType,
        string containerName,
        CancellationToken cancellationToken = default);

    Task<string> SaveDocumentAsync(
        byte[] content,
        string extension,
        string originalFileName,
        string contentType,
        string containerName,
        CancellationToken cancellationToken = default);

    Task RemoveFileAsync(
        string filePath,
        string containerName,
        CancellationToken cancellationToken = default);

    Task<string> ReplaceFileAsync(
        byte[] content,
        string extension,
        string contentType,
        string currentFilePath,
        string containerName,
        string? originalFileName,
        CancellationToken cancellationToken = default);
}
