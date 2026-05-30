using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Common;

namespace ParlorPrediction.Infrastructure.Services.Templates;

public sealed class FileTemplateProvider : ITemplateProvider
{
    private readonly IWebHostEnvironment _environment;
    private readonly TemplatePathOptions _templatePathOptions;

    public FileTemplateProvider(
        IWebHostEnvironment environment,
        IOptions<TemplatePathOptions> templatePathOptions)
    {
        _environment = environment;
        _templatePathOptions = templatePathOptions.Value;
    }

    public async Task<string> GetTemplateAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        var relativePath = templateKey switch
        {
            "EmailConfirmation" => _templatePathOptions.EmailConfirmation,
            "PasswordReset" => _templatePathOptions.PasswordReset,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new FileNotFoundException($"Template path is not configured for key '{templateKey}'.");
        }

        var fullPath = Path.Combine(_environment.ContentRootPath, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Template not found at '{fullPath}'.");
        }

        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }
}
