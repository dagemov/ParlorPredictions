using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParlorPrediction.Mvc.Helpers;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class PrepTaskFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public DateOnly TaskDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public Guid PrepItemId { get; set; }

    [Required]
    public Guid PrepStationId { get; set; }

    [Required]
    public string AssignedRole { get; set; } = string.Empty;

    [Required]
    public string TaskType { get; set; } = "GenericDough";

    [Required]
    public string QuantityUnit { get; set; } = "Balls";

    [Range(1, int.MaxValue)]
    public int QuantityValue { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsEditMode { get; set; }

    public IReadOnlyList<SelectListItem> PrepItemOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> PrepStationOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> AssignedRoleOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> TaskTypeOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> QuantityUnitOptions { get; set; } = Array.Empty<SelectListItem>();

    public string QuantityPreviewText => DoughQuantityInputConverter.BuildPlannedPreviewTextForTask(TaskType, QuantityUnit, QuantityValue);
}
