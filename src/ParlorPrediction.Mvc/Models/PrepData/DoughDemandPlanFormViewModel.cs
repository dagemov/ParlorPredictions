using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Mvc.Models.PrepData;

public sealed class DoughDemandPlanFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Tuesday;

    [Required]
    [StringLength(DoughDemandPlan.SourceNameMaxLength)]
    public string SourceName { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int MinDoughBalls { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxDoughBalls { get; set; }

    [StringLength(DoughDemandPlan.NotesMaxLength)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> DayOfWeekOptions { get; set; } = Array.Empty<SelectListItem>();

    public bool IsEditMode => Id.HasValue;
}
