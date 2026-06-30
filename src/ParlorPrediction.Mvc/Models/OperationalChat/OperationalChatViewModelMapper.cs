using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Mvc.Models.OperationalChat;

public static class OperationalChatViewModelMapper
{
    public static OperationalChatTurnViewModel MapTurn(
        OperationalChatInputViewModel input,
        OperationalChatResponse response)
    {
        return new OperationalChatTurnViewModel
        {
            CorrelationId = response.CorrelationId,
            UserMessage = input.SourceText.Trim(),
            AssistantSummary = response.NarrativeSummary,
            RequiresClarification = response.RequiresClarification,
            ClarificationPrompt = response.ClarificationPrompt,
            DetectedIntents = response.DetectedIntents
                .Select(intent => new OperationalChatDetectedIntentViewModel
                {
                    Domain = intent.Domain,
                    IntentKind = intent.IntentKind,
                    Summary = intent.Summary,
                    Decision = intent.Decision,
                    EffectiveDate = intent.EffectiveDate,
                    RequiresClarification = intent.RequiresClarification,
                    ClarificationPrompt = intent.ClarificationPrompt,
                    SourceFragment = intent.SourceFragment
                })
                .ToArray(),
            CreatedDrafts = response.CreatedDrafts
                .Select(draft => new OperationalChatCreatedDraftViewModel
                {
                    DraftId = draft.DraftId,
                    DraftType = draft.DraftType,
                    DraftTypeDisplay = FormatDraftType(draft.DraftType),
                    DraftStatus = draft.DraftStatus,
                    DraftStatusDisplay = FormatDraftStatus(draft.DraftStatus),
                    ReviewPath = draft.ReviewPath,
                    RiskLevel = draft.RiskLevel,
                    HasConflicts = draft.HasConflicts,
                    WarningCount = draft.ValidationWarnings.Count
                })
                .ToArray(),
            Warnings = response.Warnings
                .Select(warning => new OperationalChatWarningViewModel
                {
                    Code = warning.Code,
                    Message = warning.Message,
                    BlocksDraft = warning.BlocksDraft,
                    RequiresHumanReview = warning.RequiresHumanReview
                })
                .ToArray()
        };
    }

    public static OperationalChatTurnViewModel MapValidationFailure(
        OperationalChatInputViewModel input,
        string message)
    {
        return new OperationalChatTurnViewModel
        {
            CorrelationId = Guid.Empty,
            UserMessage = input.SourceText?.Trim() ?? string.Empty,
            AssistantSummary = "No drafts were created yet.",
            RequiresClarification = true,
            ClarificationPrompt = message,
            Warnings =
            [
                new OperationalChatWarningViewModel
                {
                    Code = "chat-validation",
                    Message = message,
                    RequiresHumanReview = true
                }
            ]
        };
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

    private static string FormatDraftStatus(string status)
    {
        return status switch
        {
            nameof(OperationalDraftStatus.Pending) => "Pending",
            nameof(OperationalDraftStatus.ReadyForApproval) => "Ready For Approval",
            nameof(OperationalDraftStatus.Approved) => "Approved",
            nameof(OperationalDraftStatus.Rejected) => "Rejected",
            _ => status
        };
    }
}
