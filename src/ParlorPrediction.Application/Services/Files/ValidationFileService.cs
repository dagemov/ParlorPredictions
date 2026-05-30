using SixLabors.ImageSharp;
using ParlorPrediction.Application.Interfaces.Files;

namespace ParlorPrediction.Application.Services.Files;

public sealed class ValidationFileService : IValidationFileService
{
    private static readonly HashSet<string> ValidImageExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp",
        ".webp",
        ".tiff"
    ];

    private static readonly HashSet<string> ValidDocumentExtensions =
    [
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".csv",
        ".rtf"
    ];

    public bool IsDocument(string extension)
    {
        return ValidDocumentExtensions.Contains(NormalizeExtension(extension));
    }

    public bool IsImage(string extension)
    {
        return ValidImageExtensions.Contains(NormalizeExtension(extension));
    }

    public async Task<bool> ValidateFileAsync(byte[] content, string extension, long maxSizeInBytes = 5 * 1024 * 1024)
    {
        var normalizedExtension = NormalizeExtension(extension);
        if (!IsImage(normalizedExtension) && !IsDocument(normalizedExtension))
        {
            return false;
        }

        if (content.Length == 0 || content.Length > maxSizeInBytes)
        {
            return false;
        }

        if (IsImage(normalizedExtension))
        {
            return await ValidateImageAsync(content, normalizedExtension);
        }

        return await ValidateDocumentAsync(content, normalizedExtension);
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.') ? normalized : $".{normalized}";
    }

    private static async Task<bool> ValidateImageAsync(byte[] content, string extension)
    {
        try
        {
            await using var memoryStream = new MemoryStream(content);
            using var image = await Image.LoadAsync(memoryStream);
            var format = Image.DetectFormat(content);

            if (format is null || format.Name is not ("JPEG" or "PNG" or "WEBP"))
            {
                return false;
            }

            if (image.Width <= 0 || image.Height <= 0 || image.Width > 8000 || image.Height > 8000)
            {
                return false;
            }

            return extension is ".jpg" or ".jpeg" or ".png" or ".webp";
        }
        catch
        {
            return false;
        }
    }

    private static Task<bool> ValidateDocumentAsync(byte[] content, string extension)
    {
        return Task.FromResult(extension switch
        {
            ".pdf" => content.Length >= 5 && System.Text.Encoding.ASCII.GetString(content, 0, 5) == "%PDF-",
            ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => content.Length >= 2 && content[0] == 0x50 && content[1] == 0x4B,
            _ => true
        });
    }
}
