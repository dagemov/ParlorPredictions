using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.AdminDoughCorrections;

public sealed class AdminDoughBatchCorrectionFormViewModel
{
    public Guid DoughBatchId { get; set; }

    public DateOnly ReferenceDate { get; set; }

    [Display(Name = "Batch date")]
    public DateOnly BatchDate { get; set; }

    [Range(1, int.MaxValue)]
    public int TotalCases { get; set; }

    public bool IsBalled { get; set; }

    [Display(Name = "Balled at")]
    public DateTime? BalledAtLocal { get; set; }

    public bool IsEventException { get; set; }

    public bool IsVoided { get; set; }

    public string? VoidReason { get; set; }

    public string? Notes { get; set; }
}
