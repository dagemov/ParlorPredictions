using ParlorPrediction.Domain.Enums;

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
        int quantityRecommended,
        string? notes)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetTaskDate(taskDate);
        SetPrepItem(prepItemId);
        SetPrepStation(prepStationId);
        SetRecommendation(doughPrepRecommendationId);
        AssignedRole = assignedRole;
        QuantityRecommended = EnsureNonNegative(quantityRecommended, nameof(quantityRecommended));
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

    public int QuantityRecommended { get; private set; }

    public int QuantityCompleted { get; private set; }

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

    public static PrepTask Create(
        DateOnly taskDate,
        Guid prepItemId,
        Guid prepStationId,
        ApplicationRole assignedRole,
        int quantityRecommended,
        Guid? doughPrepRecommendationId = null,
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
            quantityRecommended,
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
        QuantityRecommended = EnsureNonNegative(quantityRecommended, nameof(quantityRecommended));
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
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

    private static int EnsureNonNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
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
}
