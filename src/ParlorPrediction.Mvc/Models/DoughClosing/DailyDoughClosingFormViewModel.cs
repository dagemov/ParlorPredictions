using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class DailyDoughClosingFormViewModel
{
    public Guid? DailyDoughClosingId { get; set; }

    public bool IsEdit { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Closing Date")]
    public DateOnly ClosingDate { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Forecast Needed Balls")]
    public int ForecastNeededBalls { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Actual Dough Balls Used")]
    public int ActualUsedBalls { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [StringLength(1000)]
    [Display(Name = "Correction Note")]
    public string? CorrectionNote { get; set; }
}
