using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Domain.Entities;

public sealed class PrepTask
{
    public const int NotesMaxLength = 500;

    private PrepTask()
    {
    }

    private PrepTask(
        Guid id,
        DateOnly taskDate,
        Guid prepItemId,
        Guid prepStationId,
        Guid? doughPrepRecommendationId,
        ApplicationRole assignedRole,
        PrepTaskType taskType,
        DoughQuantityUnit quantityUnit,
        int quantityRecommended,
        Guid? sourcePrepTaskId,
        Guid? sourceDoughBatchId,
        string? notes)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetTaskDate(taskDate);
        SetPrepItem(prepItemId);
        SetPrepStation(prepStationId);
        SetRecommendation(doughPrepRecommendationId);
        SetSourcePrepTaskId(sourcePrepTaskId);
        SetSourceDoughBatchId(sourceDoughBatchId);
        AssignedRole = assignedRole;
        TaskType = taskType;
        QuantityUnit = quantityUnit;
        QuantityRecommended = EnsurePositive(quantityRecommended, nameof(quantityRecommended));
        ValidateTaskConfiguration(taskType, quantityUnit, quantityRecommended, sourcePrepTaskId, sourceDoughBatchId);
        QuantityCompleted = 0;
        Status = PrepTaskStatus.Pending;
        Notes = NormalizeOptional(notes);
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly TaskDate { get; private set; }

    public Guid PrepItemId { get; private set; }

    public Guid PrepStationId { get; private set; }

    public Guid? DoughPrepRecommendationId { get; private set; }

    public ApplicationRole AssignedRole { get; private set; }

    public PrepTaskType TaskType { get; private set; }

    public DoughQuantityUnit QuantityUnit { get; private set; }

    public int QuantityRecommended { get; private set; }

    public int QuantityCompleted { get; private set; }

    public Guid? SourcePrepTaskId { get; private set; }

    public Guid? SourceDoughBatchId { get; private set; }

    public PrepTaskStatus Status { get; private set; }

    public string? CompletedByUserId { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public PrepItem PrepItem { get; private set; } = null!;

    public PrepStation PrepStation { get; private set; } = null!;

    public DoughPrepRecommendation? DoughPrepRecommendation { get; private set; }

    public User? CompletedByUser { get; private set; }

    public PrepTask? SourcePrepTask { get; private set; }

    public DoughBatch? SourceDoughBatch { get; private set; }

    public int RecommendedBallsEquivalent => ConvertToBalls(QuantityRecommended, QuantityUnit);

    public int CompletedBallsEquivalent => ConvertToBalls(QuantityCompleted, QuantityUnit);

    public bool CountsAsAvailableBallsWhenCompleted => TaskType is not PrepTaskType.MakeDoughLoad;

    public bool CreatesBallingFollowUpWhenCompleted => TaskType == PrepTaskType.MakeDoughLoad;

    public static PrepTask Create(
        DateOnly taskDate,
        Guid prepItemId,
        Guid prepStationId,
        ApplicationRole assignedRole,
        int quantityRecommended,
        Guid? doughPrepRecommendationId = null,
        PrepTaskType taskType = PrepTaskType.GenericDough,
        DoughQuantityUnit quantityUnit = DoughQuantityUnit.Balls,
        Guid? sourcePrepTaskId = null,
        Guid? sourceDoughBatchId = null,
        string? notes = null,
        Guid? id = null)
    {
        return new PrepTask(
            id ?? Guid.Empty,
            taskDate,
            prepItemId,
            prepStationId,
            doughPrepRecommendationId,
            assignedRole,
            taskType,
            quantityUnit,
            quantityRecommended,
            sourcePrepTaskId,
            sourceDoughBatchId,
            notes);
    }

    public void MarkInProgress()
    {
        if (Status == PrepTaskStatus.Completed)
        {
            throw new InvalidOperationException("Completed tasks cannot be moved back to in progress.");
        }

        if (Status == PrepTaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled tasks cannot be started.");
        }

        if (Status == PrepTaskStatus.InProgress)
        {
            return;
        }

        Status = PrepTaskStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Complete(string completedByUserId, int quantityCompleted, string? notes = null, DateTime? completedAtUtc = null)
    {
        if (Status == PrepTaskStatus.Completed)
        {
            throw new InvalidOperationException("This prep task has already been completed.");
        }

        if (Status == PrepTaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled prep tasks cannot be completed.");
        }

        if (quantityCompleted <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityCompleted), "Quantity completed must be greater than zero.");
        }

        CompletedByUserId = NormalizeRequired(completedByUserId, nameof(completedByUserId));
        QuantityCompleted = quantityCompleted;
        Status = PrepTaskStatus.Completed;
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;

        var normalizedNotes = NormalizeOptional(notes);
        if (normalizedNotes is not null)
        {
            Notes = normalizedNotes;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string? notes = null)
    {
        if (Status == PrepTaskStatus.Completed)
        {
            throw new InvalidOperationException("Completed prep tasks cannot be cancelled.");
        }

        if (Status == PrepTaskStatus.Cancelled)
        {
            return;
        }

        Status = PrepTaskStatus.Cancelled;

        var normalizedNotes = NormalizeOptional(notes);
        if (normalizedNotes is not null)
        {
            Notes = normalizedNotes;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateTask(
        DateOnly taskDate,
        Guid prepItemId,
        Guid prepStationId,
        ApplicationRole assignedRole,
        PrepTaskType taskType,
        DoughQuantityUnit quantityUnit,
        int quantityRecommended,
        string? notes = null)
    {
        if (Status == PrepTaskStatus.Completed)
        {
            throw new InvalidOperationException("Completed prep tasks cannot be edited.");
        }

        SetTaskDate(taskDate);
        SetPrepItem(prepItemId);
        SetPrepStation(prepStationId);
        AssignedRole = assignedRole;
        TaskType = taskType;
        QuantityUnit = quantityUnit;
        QuantityRecommended = EnsurePositive(quantityRecommended, nameof(quantityRecommended));
        ValidateTaskConfiguration(TaskType, QuantityUnit, QuantityRecommended, SourcePrepTaskId, SourceDoughBatchId);
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AdminCorrect(
        DateOnly taskDate,
        PrepTaskType taskType,
        DoughQuantityUnit quantityUnit,
        int quantityRecommended,
        PrepTaskStatus status,
        int quantityCompleted,
        DateTime? completedAtUtc,
        string? completedByUserId,
        Guid? sourcePrepTaskId,
        Guid? sourceDoughBatchId,
        string? notes = null)
    {
        SetTaskDate(taskDate);
        SetSourcePrepTaskId(sourcePrepTaskId);
        SetSourceDoughBatchId(sourceDoughBatchId);
        TaskType = taskType;
        QuantityUnit = quantityUnit;
        QuantityRecommended = EnsurePositive(quantityRecommended, nameof(quantityRecommended));
        ValidateTaskConfiguration(taskType, quantityUnit, quantityRecommended, sourcePrepTaskId, sourceDoughBatchId);

        switch (status)
        {
            case PrepTaskStatus.Pending:
            case PrepTaskStatus.InProgress:
            case PrepTaskStatus.Cancelled:
                QuantityCompleted = 0;
                CompletedAtUtc = null;
                CompletedByUserId = null;
                Status = status;
                break;

            case PrepTaskStatus.Completed:
                QuantityCompleted = EnsurePositive(quantityCompleted, nameof(quantityCompleted));
                CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
                CompletedByUserId = NormalizeRequired(
                    completedByUserId ?? string.Empty,
                    nameof(completedByUserId));
                Status = status;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), "The prep task status is not supported.");
        }

        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetTaskDate(DateOnly taskDate)
    {
        if (taskDate == default)
        {
            throw new ArgumentException("Task date is required.", nameof(taskDate));
        }

        TaskDate = taskDate;
    }

    private void SetPrepItem(Guid prepItemId)
    {
        if (prepItemId == Guid.Empty)
        {
            throw new ArgumentException("Prep item id is required.", nameof(prepItemId));
        }

        PrepItemId = prepItemId;
    }

    private void SetPrepStation(Guid prepStationId)
    {
        if (prepStationId == Guid.Empty)
        {
            throw new ArgumentException("Prep station id is required.", nameof(prepStationId));
        }

        PrepStationId = prepStationId;
    }

    private void SetRecommendation(Guid? doughPrepRecommendationId)
    {
        if (doughPrepRecommendationId == Guid.Empty)
        {
            throw new ArgumentException("Recommendation id cannot be an empty guid.", nameof(doughPrepRecommendationId));
        }

        DoughPrepRecommendationId = doughPrepRecommendationId;
    }

    private void SetSourcePrepTaskId(Guid? sourcePrepTaskId)
    {
        if (sourcePrepTaskId == Guid.Empty)
        {
            throw new ArgumentException("Source prep task id cannot be an empty guid.", nameof(sourcePrepTaskId));
        }

        SourcePrepTaskId = sourcePrepTaskId;
    }

    private void SetSourceDoughBatchId(Guid? sourceDoughBatchId)
    {
        if (sourceDoughBatchId == Guid.Empty)
        {
            throw new ArgumentException("Source dough batch id cannot be an empty guid.", nameof(sourceDoughBatchId));
        }

        SourceDoughBatchId = sourceDoughBatchId;
    }

    private static int EnsurePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than zero.");
        }

        return value;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int ConvertToBalls(int quantity, DoughQuantityUnit quantityUnit)
    {
        return quantity <= 0
            ? 0
            : DoughRules.ConvertToBalls(quantity, quantityUnit);
    }

    private static void ValidateTaskConfiguration(
        PrepTaskType taskType,
        DoughQuantityUnit quantityUnit,
        int quantityRecommended,
        Guid? sourcePrepTaskId,
        Guid? sourceDoughBatchId)
    {
        if (quantityRecommended <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityRecommended), "Quantity recommended must be greater than zero.");
        }

        switch (taskType)
        {
            case PrepTaskType.GenericDough:
                if (quantityUnit != DoughQuantityUnit.Balls)
                {
                    throw new InvalidOperationException("Generic dough tasks must store quantities in balls.");
                }

                break;

            case PrepTaskType.MakeDoughLoad:
                if (quantityUnit != DoughQuantityUnit.FullLoads)
                {
                    throw new InvalidOperationException("Make dough load tasks must store quantities in full loads.");
                }

                if (sourceDoughBatchId.HasValue)
                {
                    throw new InvalidOperationException("Make dough load tasks cannot reference a source dough batch.");
                }

                break;

            case PrepTaskType.BallDough:
                if (quantityUnit != DoughQuantityUnit.Balls)
                {
                    throw new InvalidOperationException("Ball dough tasks must store quantities in balls.");
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(taskType), "The prep task type is not supported.");
        }

        if (sourcePrepTaskId == Guid.Empty)
        {
            throw new ArgumentException("Source prep task id cannot be an empty guid.", nameof(sourcePrepTaskId));
        }

        if (sourceDoughBatchId == Guid.Empty)
        {
            throw new ArgumentException("Source dough batch id cannot be an empty guid.", nameof(sourceDoughBatchId));
        }
    }
}
