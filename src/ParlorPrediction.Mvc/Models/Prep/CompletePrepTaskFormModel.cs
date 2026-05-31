using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class CompletePrepTaskFormModel
{
    [Required]
    public Guid PrepTaskId { get; set; }

    [Required]
    public DateOnly TargetDate { get; set; }

    [Range(1, int.MaxValue)]
    public int HistoricalWeeksToUse { get; set; } = 8;

    [Range(0, int.MaxValue)]
    public int QuantityCompleted { get; set; }

    [Range(0, int.MaxValue)]
    public int FullLoadsCompleted { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
