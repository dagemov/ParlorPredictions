namespace ParlorPrediction.Application.Interfaces.Files;

public interface IValidationFileService
{
    bool IsDocument(string extension);

    bool IsImage(string extension);

    Task<bool> ValidateFileAsync(byte[] content, string extension, long maxSizeInBytes = 5 * 1024 * 1024);
}
