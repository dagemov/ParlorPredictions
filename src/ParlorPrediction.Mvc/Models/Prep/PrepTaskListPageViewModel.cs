using Microsoft.AspNetCore.Mvc.Rendering;

namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class PrepTaskListPageViewModel
{
    public DateOnly? TaskDate { get; set; }

    public string? Status { get; set; }

    public string? AssignedRole { get; set; }

    public Guid? PrepItemId { get; set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> RoleOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> PrepItemOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<DoughTaskViewModel> Tasks { get; set; } = Array.Empty<DoughTaskViewModel>();

    public bool CanManageTasks { get; set; }

    public int PendingTasksCount { get; set; }

    public int CompletedTasksCount { get; set; }
}
