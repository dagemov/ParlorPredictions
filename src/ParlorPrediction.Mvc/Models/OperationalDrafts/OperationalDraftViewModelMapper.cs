using System.Text.Json;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Mvc.Models.OperationalDrafts;

public static class OperationalDraftViewModelMapper
{
    private static readonly JsonSerializerOptions PrettyJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static OperationalDraftListPageViewModel MapListPage(
        IReadOnlyList<OperationalDraftInboxItem> inboxItems)
    {
        return new OperationalDraftListPageViewModel
        {
            Drafts = inboxItems
                .Select(item => new OperationalDraftListItemViewModel
                {
                    DraftId = item.Draft.Id,
                    DraftType = item.Draft.DraftType,
                    DraftTypeDisplay = FormatDraftType(item.Draft.DraftType),
                    CreatedAtLocal = item.Draft.CreatedAtUtc.ToLocalTime(),
                    CorrelationId = item.Draft.CorrelationId.ToString(),
                    Status = item.Draft.Status.ToString(),
                    StatusDisplay = FormatStatus(item.Draft.Status),
                    RiskLevel = item.Preview.RiskLevel,
                    HasConflicts = item.Preview.HasConflicts,
                    CreatedBy = item.Draft.CreatedBy,
                    StateDriftDetected = item.Preview.StateDriftDetected
                })
                .ToArray()
        };
    }

    public static OperationalDraftDetailsPageViewModel MapDetails(
        OperationalDraftDetailResult detail)
    {
        return new OperationalDraftDetailsPageViewModel
        {
            DraftId = detail.Draft.Id,
            DraftType = detail.Draft.DraftType,
            DraftTypeDisplay = FormatDraftType(detail.Draft.DraftType),
            Status = detail.Draft.Status.ToString(),
            StatusDisplay = FormatStatus(detail.Draft.Status),
            CorrelationId = detail.Draft.CorrelationId.ToString(),
            CreatedBy = detail.Draft.CreatedBy,
            CreatedAtLocal = detail.Draft.CreatedAtUtc.ToLocalTime(),
            ReviewedByUserId = detail.Draft.ReviewedByUserId,
            ReviewedAtLocal = detail.Draft.ReviewedAtUtc?.ToLocalTime(),
            StatusReason = detail.Draft.StatusReason,
            HumanNarrative = detail.Draft.SourceText,
            NormalizedInterpretationJson = PrettyJson(detail.Draft.NormalizedIntentJson),
            BeforeSnapshotJson = PrettyJson(detail.Draft.BeforeSnapshotJson),
            AfterPreviewJson = PrettyJson(detail.Draft.AfterPreviewJson),
            AuditEntries = detail.AuditEntries
                .Select(entry => new OperationalAuditEntryViewModel
                {
                    ActionType = entry.ActionType,
                    ActorUserId = entry.ActorUserId,
                    TimestampLocal = entry.TimestampUtc.ToLocalTime(),
                    ApprovedEntityId = entry.ApprovedEntityId?.ToString(),
                    IsForCurrentDraft = entry.DraftId == detail.Draft.Id
                })
                .ToArray()
        };
    }

    public static OperationalDraftPreviewPanelViewModel MapPreviewPanel(
        OperationalDraft draft,
        OperationalPreviewResult preview)
    {
        var canApprove =
            !preview.HasConflicts &&
            !string.Equals(preview.RiskLevel, "High", StringComparison.OrdinalIgnoreCase) &&
            draft.Status is OperationalDraftStatus.Pending or OperationalDraftStatus.ReadyForApproval;

        var approvalBlockedReason = preview.HasConflicts
            ? "Approval is blocked because the preview contains conflicts."
            : string.Equals(preview.RiskLevel, "High", StringComparison.OrdinalIgnoreCase)
                ? "Approval is blocked because the preview risk level is High."
                : draft.Status is OperationalDraftStatus.Approved
                    ? "This draft has already been approved."
                    : draft.Status == OperationalDraftStatus.Rejected
                        ? "Rejected drafts cannot return to approval flow."
                        : string.Empty;

        return new OperationalDraftPreviewPanelViewModel
        {
            DraftId = draft.Id,
            DraftStatus = draft.Status.ToString(),
            DraftStatusDisplay = FormatStatus(draft.Status),
            Preview = preview,
            CanApprove = canApprove,
            CanReject = draft.Status != OperationalDraftStatus.Approved,
            ApprovalBlockedReason = approvalBlockedReason
        };
    }

    private static string PrettyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, PrettyJsonOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static string FormatDraftType(string draftType)
    {
        return draftType switch
        {
            "WeeklyCorrection" => "Weekly Correction",
            "WeeklyClosingPreview" => "Weekly Closing Preview",
            "DoughTask" => "Dough Task",
            "DailyClosing" => "Daily Closing",
            "RestaurantEvent" => "Restaurant Event",
            "ProjectionAdjustment" => "Projection Adjustment",
            _ => draftType
        };
    }

    private static string FormatStatus(OperationalDraftStatus status)
    {
        return status switch
        {
            OperationalDraftStatus.Pending => "Pending",
            OperationalDraftStatus.ReadyForApproval => "Ready For Approval",
            OperationalDraftStatus.Approved => "Approved",
            OperationalDraftStatus.Rejected => "Rejected",
            _ => status.ToString()
        };
    }
}
