using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.DoughUsage;

public sealed class DoughUsageTraceFormViewModel
{
    public Guid? DoughUsageTraceId { get; set; }

    public bool IsEdit { get; set; }

    public DateOnly UsageDate { get; set; }

    public string Destination { get; set; } = "Restaurant";

    public Guid SourceDoughBatchQualityRecordId { get; set; }

    public int TrayCount { get; set; } = 1;

    public string? Notes { get; set; }

    public int BallsUsed => Math.Max(TrayCount, 0) * DoughRules.BallsPerCase;
}
