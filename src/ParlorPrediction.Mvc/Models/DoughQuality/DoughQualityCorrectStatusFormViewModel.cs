using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityCorrectStatusFormViewModel
{
    public Guid DoughBatchQualityRecordId { get; set; }

    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Choose the corrected dough status.")]
    public string NewStatus { get; set; } = string.Empty;

    [Display(Name = "Status reason")]
    [StringLength(500, ErrorMessage = "Keep the reason under 500 characters.")]
    public string? StatusReason { get; set; }

    [Display(Name = "Effective date")]
    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Must use by")]
    public DateOnly? MustUseByDate { get; set; }

    public string? DiscardReason { get; set; }

    [StringLength(1000, ErrorMessage = "Keep the note under 1000 characters.")]
    public string? ManagerNote { get; set; }
}
