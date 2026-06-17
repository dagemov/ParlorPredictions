using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Mvc.Models.DoughUsage;

public sealed class DoughUsageTraceFormViewModel
{
    public Guid? DoughUsageTraceId { get; set; }

    public bool IsEdit { get; set; }

    public DateOnly UsageDate { get; set; }

    public string Destination { get; set; } = "Restaurant";

    public Guid SourceDoughBatchQualityRecordId { get; set; }

    public decimal TrayCount { get; set; } = 1m;

    public string? Notes { get; set; }

    public int BallsUsed
    {
        get
        {
            if (TrayCount <= 0m)
            {
                return 0;
            }

            try
            {
                return DoughRules.ConvertCaseQuantityToBalls(TrayCount);
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0;
            }
        }
    }
}
