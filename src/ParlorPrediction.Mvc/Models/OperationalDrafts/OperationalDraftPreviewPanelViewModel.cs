using ParlorPrediction.Application.Interfaces.Ai;

namespace ParlorPrediction.Mvc.Models.OperationalDrafts;

public sealed class OperationalDraftPreviewPanelViewModel
{
    public Guid DraftId { get; init; }

    public string DraftStatus { get; init; } = string.Empty;

    public string DraftStatusDisplay { get; init; } = string.Empty;

    public OperationalPreviewResult Preview { get; init; } = new();

    public bool CanApprove { get; init; }

    public bool CanReject { get; init; }

    public string ApprovalBlockedReason { get; init; } = string.Empty;
}
