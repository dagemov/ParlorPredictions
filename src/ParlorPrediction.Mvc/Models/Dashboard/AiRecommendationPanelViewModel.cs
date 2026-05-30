namespace ParlorPrediction.Mvc.Models.Dashboard;

public sealed class AiRecommendationPanelViewModel
{
    public DateOnly TargetDate { get; set; }

    public string RecommendationText { get; set; } = string.Empty;

    public bool IsAiGenerated { get; set; }

    public bool HasRecommendation => !string.IsNullOrWhiteSpace(RecommendationText);

    public string? ErrorMessage { get; set; }
}
