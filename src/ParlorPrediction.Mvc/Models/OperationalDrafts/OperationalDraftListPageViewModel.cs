namespace ParlorPrediction.Mvc.Models.OperationalDrafts;

public sealed class OperationalDraftListPageViewModel
{
    public IReadOnlyList<OperationalDraftListItemViewModel> Drafts { get; init; } = Array.Empty<OperationalDraftListItemViewModel>();
}

public sealed class OperationalDraftListItemViewModel
{
    public Guid DraftId { get; init; }

    public string DraftType { get; init; } = string.Empty;

    public string DraftTypeDisplay { get; init; } = string.Empty;

    public DateTime CreatedAtLocal { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string StatusDisplay { get; init; } = string.Empty;

    public string RiskLevel { get; init; } = string.Empty;

    public bool HasConflicts { get; init; }

    public string CreatedBy { get; init; } = string.Empty;

    public bool StateDriftDetected { get; init; }
}
