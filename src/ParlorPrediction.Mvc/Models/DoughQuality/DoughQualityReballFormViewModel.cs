using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityReballFormViewModel
{
    public Guid DoughBatchQualityRecordId { get; set; }

    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public int QuantityBeforeBalls { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Enter how many balls were recovered after reballing.")]
    public int QuantityRecoveredBalls { get; set; }

    [Display(Name = "Reball date")]
    public DateOnly ReballDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public string? LossReason { get; set; }

    [StringLength(1000, ErrorMessage = "Keep the note under 1000 characters.")]
    public string? ManagerNote { get; set; }
}
