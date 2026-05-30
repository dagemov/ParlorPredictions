using SixLabors.ImageSharp;
using ParlorPrediction.Application.Interfaces.Files;
using System.Text.RegularExpressions;

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
        ".tif",
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

    private static readonly Dictionary<string, HashSet<string>> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".gif"] = ["image/gif"],
        [".bmp"] = ["image/bmp"],
        [".webp"] = ["image/webp"],
        [".tif"] = ["image/tiff", "image/tif"],
        [".tiff"] = ["image/tiff", "image/tif"],
        [".pdf"] = ["application/pdf"],
        [".doc"] = ["application/msword", "application/octet-stream"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip"],
        [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip"],
        [".ppt"] = ["application/vnd.ms-powerpoint", "application/octet-stream"],
        [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/zip"],
        [".txt"] = ["text/plain"],
        [".csv"] = ["text/csv", "application/csv", "text/plain"],
        [".rtf"] = ["application/rtf", "text/rtf"]
    };

    public bool IsDocument(string extension)
    {
        return ValidDocumentExtensions.Contains(NormalizeExtension(extension));
    }

    public bool IsImage(string extension)
    {
        return ValidImageExtensions.Contains(NormalizeExtension(extension));
    }

    public string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return "file";
        }

        safeName = Regex.Replace(safeName, @"[^A-Za-z0-9\-_]+", "-");
        safeName = safeName.Trim('-');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "file";
        }

        return safeName.Length <= 80 ? safeName : safeName[..80];
    }

    public string GetCanonicalContentType(string extension)
    {
        var normalizedExtension = NormalizeExtension(extension);
        return AllowedContentTypes.TryGetValue(normalizedExtension, out var contentTypes)
            ? contentTypes.First()
            : "application/octet-stream";
    }

    public async Task<bool> ValidateFileAsync(
        byte[] content,
        string extension,
        string? contentType,
        long maxSizeInBytes = 5 * 1024 * 1024)
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

        if (!IsAllowedContentType(normalizedExtension, contentType))
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
            var allowedFormats = extension switch
            {
                ".jpg" or ".jpeg" => new[] { "JPEG" },
                ".png" => new[] { "PNG" },
                ".gif" => new[] { "GIF" },
                ".bmp" => new[] { "BMP" },
                ".webp" => new[] { "WEBP" },
                ".tif" or ".tiff" => new[] { "TIFF", "TIF" },
                _ => Array.Empty<string>()
            };

            if (format is null || allowedFormats.Length == 0 || !allowedFormats.Contains(format.Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (image.Width <= 0 || image.Height <= 0 || image.Width > 8000 || image.Height > 8000)
            {
                return false;
            }

            return true;
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
            ".docx" or ".xlsx" or ".pptx" => content.Length >= 2 && content[0] == 0x50 && content[1] == 0x4B,
            ".doc" => content.Length >= 8 && content[0] == 0xD0 && content[1] == 0xCF && content[2] == 0x11 && content[3] == 0xE0,
            ".xls" => content.Length >= 8 && content[0] == 0xD0 && content[1] == 0xCF && content[2] == 0x11 && content[3] == 0xE0,
            ".ppt" => content.Length >= 8 && content[0] == 0xD0 && content[1] == 0xCF && content[2] == 0x11 && content[3] == 0xE0,
            _ => true
        });
    }

    private static bool IsAllowedContentType(string extension, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var normalizedExtension = NormalizeExtension(extension);
        var normalizedContentType = contentType.Trim().ToLowerInvariant();

        return AllowedContentTypes.TryGetValue(normalizedExtension, out var allowedContentTypes)
            && allowedContentTypes.Contains(normalizedContentType);
    }
}
