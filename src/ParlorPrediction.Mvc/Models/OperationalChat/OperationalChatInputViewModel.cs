using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.OperationalChat;

public sealed class OperationalChatInputViewModel
{
    [Required]
    [Display(Name = "Operational Narrative")]
    public string SourceText { get; set; } = string.Empty;

    [Display(Name = "Reference Date")]
    [DataType(DataType.Date)]
    public DateOnly ReferenceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Target Week Start")]
    [DataType(DataType.Date)]
    public DateOnly? TargetWeekStartDate { get; set; }
}
