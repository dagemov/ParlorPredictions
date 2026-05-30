using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using ParlorPrediction.Application.Interfaces.Files;

namespace ParlorPrediction.Infrastructure.Services.Storage;

public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly string _connectionString;

    public AzureBlobFileStorage(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException("ConnectionStrings:AzureStorage is required.");
    }

    public async Task<string> SaveFileAsync(
        byte[] content,
        string extension,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        var client = await GetContainerClientAsync(containerName, cancellationToken);
        var fileName = $"{Guid.NewGuid()}{NormalizeExtension(extension)}";
        var blob = client.GetBlobClient(fileName);

        using var memoryStream = new MemoryStream(content);
        await blob.UploadAsync(memoryStream, overwrite: false, cancellationToken);
        return blob.Uri.ToString();
    }

    public async Task<string> SaveDocumentAsync(
        byte[] content,
        string extension,
        string originalFileName,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        var client = await GetContainerClientAsync(containerName, cancellationToken);
        var safeFileName = Path.GetFileNameWithoutExtension(originalFileName);
        var fileName = $"{safeFileName}_{Guid.NewGuid()}{NormalizeExtension(extension)}";
        var blob = client.GetBlobClient(fileName);

        using var memoryStream = new MemoryStream(content);
        await blob.UploadAsync(memoryStream, overwrite: false, cancellationToken);
        return blob.Uri.ToString();
    }

    public async Task RemoveFileAsync(
        string filePath,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        var client = await GetContainerClientAsync(containerName, cancellationToken);
        var fileName = ExtractBlobName(filePath);
        var blob = client.GetBlobClient(fileName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<string> ReplaceFileAsync(
        byte[] content,
        string extension,
        string currentFilePath,
        string containerName,
        string? originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(currentFilePath))
        {
            await RemoveFileAsync(currentFilePath, containerName, cancellationToken);
        }

        return IsDocumentExtension(extension)
            ? await SaveDocumentAsync(content, extension, originalFileName ?? "document", containerName, cancellationToken)
            : await SaveFileAsync(content, extension, containerName, cancellationToken);
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(string containerName, CancellationToken cancellationToken)
    {
        var client = new BlobContainerClient(_connectionString, containerName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await client.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        return client;
    }

    private static string ExtractBlobName(string filePath)
    {
        return Uri.TryCreate(filePath, UriKind.Absolute, out var uri)
            ? Path.GetFileName(uri.AbsolutePath)
            : Path.GetFileName(filePath);
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

    private static bool IsDocumentExtension(string extension)
    {
        return NormalizeExtension(extension) switch
        {
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".csv" or ".rtf" => true,
            _ => false
        };
    }
}
