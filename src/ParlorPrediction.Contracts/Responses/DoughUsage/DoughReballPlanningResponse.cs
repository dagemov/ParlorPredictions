namespace ParlorPrediction.Contracts.Responses.DoughUsage;

public sealed class DoughReballPlanningResponse
{
    public DateOnly ReferenceDate { get; set; }

    public IReadOnlyList<DoughSourceRemainingResponse> MustUseFirstSources { get; set; } = Array.Empty<DoughSourceRemainingResponse>();

    public IReadOnlyList<DoughSourceRemainingResponse> ReviewSources { get; set; } = Array.Empty<DoughSourceRemainingResponse>();

    public IReadOnlyList<DoughSourceRemainingResponse> ReballCandidates { get; set; } = Array.Empty<DoughSourceRemainingResponse>();

    public IReadOnlyList<DoughSourceRemainingResponse> DiscardCandidates { get; set; } = Array.Empty<DoughSourceRemainingResponse>();
}
