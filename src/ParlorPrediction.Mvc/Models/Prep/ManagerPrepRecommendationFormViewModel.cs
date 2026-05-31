using System.ComponentModel.DataAnnotations;
using ParlorPrediction.Mvc.Helpers;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class ManagerPrepRecommendationFormViewModel
{
    [Required]
    public DateOnly RecommendationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public Guid PrepItemId { get; set; }

    [Required]
    public string QuantityUnit { get; set; } = "Balls";

    [Range(0, int.MaxValue)]
    public int QuantityValue { get; set; }

    [Required]
    [StringLength(1000)]
    public string RecommendationText { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public string QuantityPreviewText => DoughQuantityInputConverter.BuildRecommendedPreviewText(QuantityUnit, QuantityValue);
}
