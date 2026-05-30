using System.ComponentModel.DataAnnotations;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Mvc.Models.PrepData;

public sealed class RestaurantEventFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(RestaurantEvent.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(0, int.MaxValue)]
    public int EstimatedPizzas { get; set; }

    [Range(0, int.MaxValue)]
    public int EstimatedDoughBalls { get; set; }

    public bool AllowShortFermentation { get; set; }

    [StringLength(RestaurantEvent.NotesMaxLength)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsEditMode => Id.HasValue;
}
