namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IAiTextGenerationProvider
{
    Task<string> GenerateTextAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
