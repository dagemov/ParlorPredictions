using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Domain.Entities;

public sealed class DoughBatch
{
    public const int DefaultBallsPerCase = DoughRules.BallsPerCase;
    public const int StandardLoadCases = DoughRules.StandardBatchCases;
    public const int MinimumFermentationDays = DoughRules.NormalFermentationMinimumDays;
    public const int NotesMaxLength = 500;

    private DoughBatch()
    {
    }

    public DoughBatch(
        Guid id,
        DateOnly batchDate,
        int totalCases,
        int ballsPerCase = DefaultBallsPerCase,
        bool isEventException = false,
        string? notes = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        SetBatchDate(batchDate);
        SetTotals(totalCases, ballsPerCase);
        IsEventException = isEventException;
        Notes = NormalizeOptional(notes);
        IsBalled = false;
        BalledAtUtc = null;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public DateOnly BatchDate { get; private set; }

    public int TotalCases { get; private set; }

    public int BallsPerCase { get; private set; }

    public int TotalBalls { get; private set; }

    public DateOnly FermentationReadyDate { get; private set; }

    public bool IsBalled { get; private set; }

    public DateTime? BalledAtUtc { get; private set; }

    public bool IsEventException { get; private set; }

    public string? Notes { get; private set; }

    public bool IsVoided { get; private set; }

    public DateTime? VoidedAtUtc { get; private set; }

    public string? VoidReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void MarkAsBalled(DateTime balledAtUtc)
    {
        if (balledAtUtc == default)
        {
            throw new ArgumentException("Balled timestamp is required.", nameof(balledAtUtc));
        }

        if (DateOnly.FromDateTime(balledAtUtc) < BatchDate)
        {
            throw new ArgumentException("Balled timestamp cannot be earlier than the batch date.", nameof(balledAtUtc));
        }

        IsBalled = true;
        BalledAtUtc = balledAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsUnballed()
    {
        IsBalled = false;
        BalledAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CorrectBatch(
        DateOnly batchDate,
        int totalCases,
        bool isBalled,
        DateTime? balledAtUtc,
        bool isEventException,
        string? notes)
    {
        SetBatchDate(batchDate);
        SetTotals(totalCases, BallsPerCase);
        IsEventException = isEventException;
        Notes = NormalizeOptional(notes);

        if (isBalled)
        {
            MarkAsBalled(balledAtUtc ?? batchDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }
        else
        {
            MarkAsUnballed();
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Void(string? reason = null, DateTime? voidedAtUtc = null)
    {
        IsVoided = true;
        VoidedAtUtc = voidedAtUtc ?? DateTime.UtcNow;
        VoidReason = NormalizeOptional(reason);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsVoided = false;
        VoidedAtUtc = null;
        VoidReason = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetBatchDate(DateOnly batchDate)
    {
        if (batchDate == default)
        {
            throw new ArgumentException("Batch date is required.", nameof(batchDate));
        }

        BatchDate = batchDate;
        FermentationReadyDate = batchDate.AddDays(MinimumFermentationDays);
    }

    private void SetTotals(int totalCases, int ballsPerCase)
    {
        if (totalCases <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCases), "Total cases must be greater than zero.");
        }

        if (ballsPerCase <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ballsPerCase), "Balls per case must be greater than zero.");
        }

        TotalCases = totalCases;
        BallsPerCase = ballsPerCase;
        TotalBalls = checked(totalCases * ballsPerCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
