using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class WeeklyDoughClosingFormViewModel
{
    public Guid? WeeklyDoughClosingId { get; set; }

    public bool IsEdit { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Week Start Date")]
    public DateOnly WeekStartDate { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Needed Balls")]
    public int NeededBalls { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Produced Balls")]
    public int ProducedBalls { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Used Balls")]
    public int UsedBalls { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Lost Balls")]
    public int LostBalls { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Leftover Ready Balls")]
    public int LeftoverReadyBalls { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Leftover Attention Balls")]
    public int LeftoverAttentionBalls { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Leftover Mixed Loads")]
    public int LeftoverMixedLoads { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [StringLength(1000)]
    [Display(Name = "Correction Note")]
    public string? CorrectionNote { get; set; }

    public DateOnly WeekEndDate => WeekStartDate == default
        ? default
        : WeekStartDate.AddDays(5);
}
