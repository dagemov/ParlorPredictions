namespace ParlorPrediction.Mvc.Models.DoughUsage;

public sealed class DoughUsageTraceListItemViewModel
{
    public Guid Id { get; set; }

    public DateOnly UsageDate { get; set; }

    public DateOnly SourceDate { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public decimal TrayCount { get; set; }

    public int BallsUsed { get; set; }

    public string? Notes { get; set; }

    public bool CanManage { get; set; }

    public string DisplayDestination => DoughUsageDisplayText.Format(Destination);

    public string DisplaySourceType => DoughUsageDisplayText.Format(SourceType);

    public string DisplayTrayCount => TrayCount.ToString("0.##");

    public string CaseLabel => TrayCount == 1m ? "case" : "cases";
}
