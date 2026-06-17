using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.AdminDoughCorrections;

public sealed class AdminPrepTaskCorrectionFormViewModel
{
    public Guid PrepTaskId { get; set; }

    public DateOnly ReferenceDate { get; set; }

    [Display(Name = "Work date")]
    public DateOnly TaskDate { get; set; }

    [Required]
    public string TaskType { get; set; } = string.Empty;

    [Required]
    public string QuantityUnit { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int QuantityRecommended { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int QuantityCompleted { get; set; }

    [Display(Name = "Completed at")]
    public DateTime? CompletedAtLocal { get; set; }

    [Display(Name = "Completed by user id")]
    public string? CompletedByUserId { get; set; }

    [Display(Name = "Source task id")]
    public Guid? SourcePrepTaskId { get; set; }

    [Display(Name = "Source batch id")]
    public Guid? SourceDoughBatchId { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyList<string> TaskTypeOptions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> QuantityUnitOptions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> StatusOptions { get; set; } = Array.Empty<string>();
}
