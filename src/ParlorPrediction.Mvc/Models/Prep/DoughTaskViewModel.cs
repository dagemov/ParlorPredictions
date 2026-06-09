using ParlorPrediction.Mvc.Helpers;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class DoughTaskViewModel
{
    public Guid PrepTaskId { get; set; }

    public Guid? DoughPrepRecommendationId { get; set; }

    public DateOnly TaskDate { get; set; }

    public Guid PrepItemId { get; set; }

    public string PrepItemCode { get; set; } = string.Empty;

    public Guid PrepStationId { get; set; }

    public string PrepStationCode { get; set; } = string.Empty;

    public int HistoricalWeeksToUse { get; set; } = 8;

    public string PrepItemName { get; set; } = string.Empty;

    public string PrepStationName { get; set; } = string.Empty;

    public string AssignedRole { get; set; } = string.Empty;

    public string TaskType { get; set; } = string.Empty;

    public string QuantityUnit { get; set; } = string.Empty;

    public int QuantityRecommended { get; set; }

    public int QuantityCompleted { get; set; }

    public int QuantityRecommendedBallsEquivalent { get; set; }

    public int QuantityCompletedBallsEquivalent { get; set; }

    public bool CountsAsAvailableBallsWhenCompleted { get; set; }

    public Guid? SourcePrepTaskId { get; set; }

    public Guid? SourceDoughBatchId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string? CompletedByUserId { get; set; }

    public string? CompletedByUserName { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsManualTask { get; set; }

    public bool CanComplete { get; set; }

    public bool CanManage { get; set; }

    public bool IsLoadTask => string.Equals(TaskType, "MakeDoughLoad", StringComparison.OrdinalIgnoreCase);

    public bool IsBallTask => string.Equals(TaskType, "BallDough", StringComparison.OrdinalIgnoreCase);

    public bool NeedsAttentionNow => IsBallTask && CanComplete;

    public string TaskTypeDisplayText => IsLoadTask
        ? "Make Dough Load"
        : IsBallTask
            ? "Ball Dough"
            : "Dough Task";

    public string TaskTitle => IsLoadTask
        ? $"Make {QuantityRecommended} full dough load{(QuantityRecommended == 1 ? string.Empty : "s")}"
        : IsBallTask
            ? $"Ball {QuantityRecommendedBallsEquivalent} dough balls"
            : PrepItemName;

    public string TaskDescription => IsLoadTask
        ? "We need this dough load today so it can be balled tomorrow."
        : IsBallTask
            ? "This dough load is ready to be balled today."
            : "Count finished dough work against today's kitchen plan.";

    public string TaskHelperText => IsLoadTask
        ? "This does not count as available balls yet."
        : IsBallTask
            ? "These balls count as available only after this task is completed."
            : "Finished dough here counts toward the current day.";

    public string RecommendedQuantityText => IsLoadTask
        ? $"{QuantityRecommended} load{(QuantityRecommended == 1 ? string.Empty : "s")}"
        : $"{QuantityRecommendedBallsEquivalent} balls";

    public string CompletedQuantityText => IsLoadTask
        ? $"{QuantityCompleted} load{(QuantityCompleted == 1 ? string.Empty : "s")}"
        : $"{QuantityCompletedBallsEquivalent} balls";

    public string SecondaryQuantityText => IsLoadTask
        ? $"Potential: {QuantityRecommendedBallsEquivalent} balls tomorrow"
        : CountsAsAvailableBallsWhenCompleted
            ? "Counts as available after completed"
            : "Does not count as available yet";

    public string StatusDetailText => IsLoadTask
        ? (string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase)
            ? $"Created {QuantityCompletedBallsEquivalent} potential balls for tomorrow."
            : "For tomorrow")
        : IsBallTask
            ? "Needs attention now"
            : (IsManualTask ? "Manual task" : "From saved dough guidance");

    public string DefaultCompletionUnit => IsLoadTask ? "FullLoads" : "Balls";

    public int SuggestedCompletionQuantityValue => IsLoadTask
        ? Math.Max(QuantityRecommended - QuantityCompleted, 1)
        : Math.Max(QuantityRecommendedBallsEquivalent - QuantityCompletedBallsEquivalent, 1);

    public bool RestrictCompletionUnit => IsLoadTask || IsBallTask;

    public string CompletionPreviewText => DoughQuantityInputConverter.BuildCompletedPreviewTextForTask(
        TaskType,
        DefaultCompletionUnit,
        SuggestedCompletionQuantityValue);
}
