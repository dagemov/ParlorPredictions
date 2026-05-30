using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Ai;
using ParlorPrediction.Contracts.Responses.Ai;

namespace ParlorPrediction.Application.Services.Ai;

public sealed class AiPrepRecommendationService : IAiPrepRecommendationService
{
    private const string DeterministicProviderName = "Deterministic";

    private readonly AiOptions _aiOptions;
    private readonly IAiTextGenerationProvider _aiTextGenerationProvider;
    private readonly ILogger<AiPrepRecommendationService> _logger;
    private readonly IPrepDashboardReadService _prepDashboardReadService;

    public AiPrepRecommendationService(
        IPrepDashboardReadService prepDashboardReadService,
        IAiTextGenerationProvider aiTextGenerationProvider,
        IOptions<AiOptions> aiOptions,
        ILogger<AiPrepRecommendationService> logger)
    {
        _prepDashboardReadService = prepDashboardReadService;
        _aiTextGenerationProvider = aiTextGenerationProvider;
        _logger = logger;
        _aiOptions = aiOptions.Value;
    }

    public async Task<AiPrepRecommendationResponse> GenerateAsync(
        AiPrepRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        var targetDate = request.TargetDate == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : request.TargetDate;

        var dashboardSummary = await _prepDashboardReadService.GetSummaryAsync(targetDate, cancellationToken);

        var providerName = NormalizeProviderName(_aiOptions.Provider);
        if (!string.Equals(providerName, DeterministicProviderName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "AI provider {ProviderName} is not available yet. Falling back to deterministic guidance.",
                _aiOptions.Provider);
        }

        var prompt = BuildPrompt(dashboardSummary);
        var recommendationText = await _aiTextGenerationProvider.GenerateTextAsync(prompt, cancellationToken);

        return new AiPrepRecommendationResponse
        {
            TargetDate = targetDate,
            RecommendationText = recommendationText,
            IsAiGenerated = false
        };
    }

    private static string BuildPrompt(Contracts.Responses.Prep.PrepDashboardSummaryResponse summary)
    {
        return string.Join(
            Environment.NewLine,
            [
                "You are preparing a dough recommendation for a restaurant manager.",
                "Use only the operational summary below.",
                "Do not suggest database writes, automatic inventory updates, or automatic task creation.",
                $"Date: {summary.TargetDate:yyyy-MM-dd}",
                $"HasRecommendation: {summary.HasRecommendation}",
                $"RequiredBalls: {summary.RequiredBalls}",
                $"AvailableBalls: {summary.AvailableBalls}",
                $"MissingBalls: {summary.MissingBalls}",
                $"RecommendedCases: {summary.RecommendedCases}",
                $"RecommendedLoads: {summary.RecommendedLoads}",
                $"PendingTasks: {summary.PendingTasks}",
                $"CompletedTasks: {summary.CompletedTasks}",
                $"LastRecommendationReason: {summary.LastRecommendationReason ?? "None"}"
            ]);
    }

    private static string NormalizeProviderName(string? providerName)
    {
        var normalized = providerName?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? DeterministicProviderName
            : normalized;
    }
}
