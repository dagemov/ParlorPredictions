namespace ParlorPrediction.Contracts.Responses.Ai;

public sealed class AiPrepRecommendationResponse
{
    public DateOnly TargetDate { get; init; }

    public string RecommendationText { get; init; } = string.Empty;

    public bool IsAiGenerated { get; init; }
}
