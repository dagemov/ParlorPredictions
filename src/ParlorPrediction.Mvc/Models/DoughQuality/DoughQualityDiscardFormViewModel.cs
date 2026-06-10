using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.DoughQuality;

public sealed class DoughQualityDiscardFormViewModel
{
    public Guid DoughBatchQualityRecordId { get; set; }

    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Discard date")]
    public DateOnly DiscardDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Choose why this dough must be discarded.")]
    public string DiscardReason { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Keep the note under 1000 characters.")]
    public string? ManagerNote { get; set; }
}
