namespace ParlorPrediction.Application.Interfaces.Files;

public interface IValidationFileService
{
    bool IsDocument(string extension);

    bool IsImage(string extension);

    string SanitizeFileName(string fileName);

    string GetCanonicalContentType(string extension);

    Task<bool> ValidateFileAsync(
        byte[] content,
        string extension,
        string? contentType,
        long maxSizeInBytes = 5 * 1024 * 1024);
}
