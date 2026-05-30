namespace ParlorPrediction.Application.Interfaces.Common;

public interface ITemplateProvider
{
    Task<string> GetTemplateAsync(string templateKey, CancellationToken cancellationToken = default);
}
