using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Dough;

public sealed class CalculateDoughPrepRequest
{
    [DataType(DataType.Date)]
    public DateOnly TargetDate { get; set; }

    [Range(1, int.MaxValue)]
    public int HistoricalWeeksToUse { get; set; } = 8;
}
