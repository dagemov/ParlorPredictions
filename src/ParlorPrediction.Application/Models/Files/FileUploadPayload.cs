namespace ParlorPrediction.Application.Models.Files;

public sealed class FileUploadPayload
{
    public byte[] Content { get; set; } = [];

    public string Extension { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "images";
}
