using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Helpers;
using ParlorPrediction.Mvc.Models.Prep;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)},{nameof(ApplicationRole.PizzaMaker)}")]
[Route("prep/tasks")]
public sealed class PrepTasksController : Controller
{
    private const string StatusTypeKey = "PrepTaskStatusType";
    private const string StatusMessageKey = "PrepTaskStatusMessage";

    private readonly IPrepCatalogReadService _prepCatalogReadService;
    private readonly IPrepTaskReadService _prepTaskReadService;
    private readonly IPrepTaskService _prepTaskService;

    public PrepTasksController(
        IPrepCatalogReadService prepCatalogReadService,
        IPrepTaskReadService prepTaskReadService,
        IPrepTaskService prepTaskService)
    {
        _prepCatalogReadService = prepCatalogReadService;
        _prepTaskReadService = prepTaskReadService;
        _prepTaskService = prepTaskService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        DateOnly? taskDate,
        string? status,
        string? assignedRole,
        Guid? prepItemId,
        CancellationToken cancellationToken = default)
    {
        var model = await BuildListPageViewModelAsync(
            new SearchPrepTasksRequest
            {
                TaskDate = taskDate,
                Status = status,
                AssignedRole = assignedRole,
                PrepItemId = prepItemId
            },
            cancellationToken);

        return View(model);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _prepTaskReadService.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        return View(new PrepTaskDetailsViewModel
        {
            Task = MapTask(task),
            CanManageTasks = CanManageTasks()
        });
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var catalogOptions = await _prepCatalogReadService.GetActiveOptionsAsync(cancellationToken);
        var defaultPrepItem = catalogOptions.PrepItems.FirstOrDefault();

        var model = await BuildFormViewModelAsync(
            new PrepTaskFormViewModel
            {
                TaskDate = DateOnly.FromDateTime(DateTime.Today),
                PrepItemId = defaultPrepItem?.Id ?? Guid.Empty,
                PrepStationId = defaultPrepItem?.PrepStationId ?? Guid.Empty,
                AssignedRole = nameof(ApplicationRole.PizzaMaker),
                TaskType = nameof(PrepTaskType.GenericDough),
                QuantityUnit = nameof(Domain.Enums.DoughQuantityUnit.Balls)
            },
            cancellationToken);

        return View("Form", model);
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        PrepTaskFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormViewModelAsync(model, cancellationToken));
        }

        try
        {
            var response = await _prepTaskService.CreateManualAsync(
                new SavePrepTaskRequest
                {
                    TaskDate = model.TaskDate,
                    PrepItemId = model.PrepItemId,
                    PrepStationId = model.PrepStationId,
                    AssignedRole = model.AssignedRole,
                    TaskType = model.TaskType,
                    QuantityUnit = model.QuantityUnit,
                    QuantityValue = model.QuantityValue,
                    Notes = model.Notes
                },
                cancellationToken);

            SetStatusMessage("success", response.Message);
            return RedirectToAction(nameof(Index), new { taskDate = model.TaskDate.ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (IsFriendlyTaskException(exception))
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", await BuildFormViewModelAsync(model, cancellationToken));
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _prepTaskReadService.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        var model = await BuildFormViewModelAsync(
            new PrepTaskFormViewModel
            {
                Id = task.PrepTaskId,
                IsEditMode = true,
                TaskDate = task.TaskDate,
                PrepItemId = task.PrepItemId,
                PrepStationId = task.PrepStationId,
                AssignedRole = task.AssignedRole,
                TaskType = task.TaskType,
                QuantityUnit = task.QuantityUnit,
                QuantityValue = task.QuantityRecommended,
                Notes = task.Notes
            },
            cancellationToken);

        return View("Form", model);
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        PrepTaskFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;
        model.IsEditMode = true;

        if (!ModelState.IsValid)
        {
            return View("Form", await BuildFormViewModelAsync(model, cancellationToken));
        }

        try
        {
            var response = await _prepTaskService.UpdateManualAsync(
                id,
                new SavePrepTaskRequest
                {
                    TaskDate = model.TaskDate,
                    PrepItemId = model.PrepItemId,
                    PrepStationId = model.PrepStationId,
                    AssignedRole = model.AssignedRole,
                    TaskType = model.TaskType,
                    QuantityUnit = model.QuantityUnit,
                    QuantityValue = model.QuantityValue,
                    Notes = model.Notes
                },
                cancellationToken);

            SetStatusMessage("success", response.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception) when (IsFriendlyTaskException(exception))
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", await BuildFormViewModelAsync(model, cancellationToken));
        }
    }

    [HttpPost("complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(
        CompletePrepTaskFormModel model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        var task = await _prepTaskReadService.GetByIdAsync(model.PrepTaskId, cancellationToken);
        if (task is null)
        {
            return NotFound();
        }

        if (User.IsInRole(nameof(ApplicationRole.PizzaMaker)) &&
            !User.IsInRole(nameof(ApplicationRole.Manager)) &&
            !User.IsInRole(nameof(ApplicationRole.Admin)) &&
            !string.Equals(task.AssignedRole, nameof(ApplicationRole.PizzaMaker), StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        try
        {
            var response = await _prepTaskService.CompleteAsync(
                new CompletePrepTaskRequest
                {
                    PrepTaskId = model.PrepTaskId,
                    CompletedByUserId = currentUserId,
                    QuantityUnit = model.CompletionType,
                    QuantityValue = model.QuantityValue,
                    Notes = model.Notes
                },
                cancellationToken);

            SetStatusMessage("success", response.Message);
        }
        catch (Exception exception) when (IsFriendlyTaskException(exception))
        {
            SetStatusMessage("danger", exception.Message);
        }

        return RedirectToAction(nameof(Index), new { taskDate = model.TargetDate.ToString("yyyy-MM-dd") });
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid id,
        DateOnly? taskDate,
        string? status,
        string? assignedRole,
        Guid? prepItemId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _prepTaskService.DeleteAsync(id, cancellationToken);
            SetStatusMessage("success", "Prep task deleted.");
        }
        catch (Exception exception) when (IsFriendlyTaskException(exception))
        {
            SetStatusMessage("danger", exception.Message);
        }

        return RedirectToAction(nameof(Index), new { taskDate, status, assignedRole, prepItemId });
    }

    private async Task<PrepTaskListPageViewModel> BuildListPageViewModelAsync(
        SearchPrepTasksRequest request,
        CancellationToken cancellationToken)
    {
        var tasks = await _prepTaskReadService.SearchAsync(request, cancellationToken);
        var catalogOptions = await _prepCatalogReadService.GetActiveOptionsAsync(cancellationToken);

        return new PrepTaskListPageViewModel
        {
            TaskDate = request.TaskDate,
            Status = request.Status,
            AssignedRole = request.AssignedRole,
            PrepItemId = request.PrepItemId,
            Tasks = tasks.Select(MapTask).ToArray(),
            CanManageTasks = CanManageTasks(),
            PendingTasksCount = tasks.Count(task =>
                string.Equals(task.Status, nameof(PrepTaskStatus.Pending), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(task.Status, nameof(PrepTaskStatus.InProgress), StringComparison.OrdinalIgnoreCase)),
            CompletedTasksCount = tasks.Count(task =>
                string.Equals(task.Status, nameof(PrepTaskStatus.Completed), StringComparison.OrdinalIgnoreCase)),
            StatusOptions = BuildStatusOptions(request.Status),
            RoleOptions = BuildRoleOptions(request.AssignedRole, includeAllOption: true),
            PrepItemOptions = BuildPrepItemOptions(catalogOptions, request.PrepItemId, includeAllOption: true)
        };
    }

    private async Task<PrepTaskFormViewModel> BuildFormViewModelAsync(
        PrepTaskFormViewModel model,
        CancellationToken cancellationToken)
    {
        var catalogOptions = await _prepCatalogReadService.GetActiveOptionsAsync(cancellationToken);
        model.PrepItemOptions = BuildPrepItemOptions(catalogOptions, model.PrepItemId, includeAllOption: false);
        model.PrepStationOptions = BuildPrepStationOptions(catalogOptions, model.PrepStationId);
        model.AssignedRoleOptions = BuildRoleOptions(model.AssignedRole, includeAllOption: false);
        model.TaskTypeOptions = BuildTaskTypeOptions(model.TaskType);
        model.QuantityUnitOptions = BuildQuantityUnitOptions(model.TaskType, model.QuantityUnit);
        return model;
    }

    private DoughTaskViewModel MapTask(DoughTaskListItemResponse task)
    {
        return new DoughTaskViewModel
        {
            PrepTaskId = task.PrepTaskId,
            DoughPrepRecommendationId = task.DoughPrepRecommendationId,
            TaskDate = task.TaskDate,
            PrepItemId = task.PrepItemId,
            PrepItemName = task.PrepItemName,
            PrepItemCode = task.PrepItemCode,
            PrepStationId = task.PrepStationId,
            PrepStationName = task.PrepStationName,
            PrepStationCode = task.PrepStationCode,
            AssignedRole = task.AssignedRole,
            TaskType = task.TaskType,
            QuantityUnit = task.QuantityUnit,
            QuantityRecommended = task.QuantityRecommended,
            QuantityCompleted = task.QuantityCompleted,
            QuantityRecommendedBallsEquivalent = task.QuantityRecommendedBallsEquivalent,
            QuantityCompletedBallsEquivalent = task.QuantityCompletedBallsEquivalent,
            CountsAsAvailableBallsWhenCompleted = task.CountsAsAvailableBallsWhenCompleted,
            SourcePrepTaskId = task.SourcePrepTaskId,
            SourceDoughBatchId = task.SourceDoughBatchId,
            Status = task.Status,
            Notes = task.Notes,
            CompletedByUserId = task.CompletedByUserId,
            CompletedByUserName = task.CompletedByUserName,
            CompletedAtUtc = task.CompletedAtUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            IsManualTask = task.IsManualTask,
            CanComplete = CanCompleteTask(task),
            CanManage = CanManageTasks()
        };
    }

    private bool CanManageTasks()
    {
        return User.IsInRole(nameof(ApplicationRole.Admin)) || User.IsInRole(nameof(ApplicationRole.Manager));
    }

    private bool CanCompleteTask(DoughTaskListItemResponse task)
    {
        if (string.Equals(task.Status, nameof(PrepTaskStatus.Completed), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(task.Status, nameof(PrepTaskStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (CanManageTasks())
        {
            return true;
        }

        return User.IsInRole(nameof(ApplicationRole.PizzaMaker)) &&
            string.Equals(task.AssignedRole, nameof(ApplicationRole.PizzaMaker), StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<SelectListItem> BuildStatusOptions(string? selectedStatus)
    {
        var normalizedStatus = selectedStatus?.Trim() ?? string.Empty;

        var items = new List<SelectListItem>
        {
            new("All statuses", string.Empty, string.IsNullOrWhiteSpace(normalizedStatus))
        };

        items.AddRange(
            Enum.GetNames<PrepTaskStatus>()
                .Where(statusName => !string.Equals(statusName, nameof(PrepTaskStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
                .Select(statusName => new SelectListItem(
                    statusName,
                    statusName,
                    string.Equals(statusName, normalizedStatus, StringComparison.OrdinalIgnoreCase))));

        return items;
    }

    private IReadOnlyList<SelectListItem> BuildRoleOptions(string? selectedRole, bool includeAllOption)
    {
        var normalizedSelectedRole = selectedRole?.Trim() ?? string.Empty;
        var roles = CanManageTasks()
            ? GetAssignableRolesForManager()
            : new[] { nameof(ApplicationRole.PizzaMaker) };

        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("All roles", string.Empty, string.IsNullOrWhiteSpace(normalizedSelectedRole)));
        }

        items.AddRange(
            roles.Select(roleName => new SelectListItem(
                roleName,
                roleName,
                string.Equals(roleName, normalizedSelectedRole, StringComparison.OrdinalIgnoreCase))));

        return items;
    }

    private static IReadOnlyList<SelectListItem> BuildPrepItemOptions(
        PrepCatalogOptionsResponse catalogOptions,
        Guid? selectedPrepItemId,
        bool includeAllOption)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("All prep items", string.Empty, !selectedPrepItemId.HasValue || selectedPrepItemId == Guid.Empty));
        }

        items.AddRange(
            catalogOptions.PrepItems.Select(item => new SelectListItem(
                item.Name,
                item.Id.ToString(),
                selectedPrepItemId == item.Id)));

        return items;
    }

    private static IReadOnlyList<SelectListItem> BuildPrepStationOptions(
        PrepCatalogOptionsResponse catalogOptions,
        Guid selectedPrepStationId)
    {
        return catalogOptions.PrepStations
            .Select(station => new SelectListItem(
                station.Name,
                station.Id.ToString(),
                selectedPrepStationId == station.Id))
            .ToArray();
    }

    private static IReadOnlyList<SelectListItem> BuildTaskTypeOptions(string? selectedTaskType)
    {
        var normalizedSelectedTaskType = selectedTaskType?.Trim() ?? nameof(PrepTaskType.GenericDough);

        var items = new List<SelectListItem>
        {
            new("Generic Dough Task", nameof(PrepTaskType.GenericDough), string.Equals(normalizedSelectedTaskType, nameof(PrepTaskType.GenericDough), StringComparison.OrdinalIgnoreCase)),
            new("Make Dough Load", nameof(PrepTaskType.MakeDoughLoad), string.Equals(normalizedSelectedTaskType, nameof(PrepTaskType.MakeDoughLoad), StringComparison.OrdinalIgnoreCase))
        };

        if (string.Equals(normalizedSelectedTaskType, nameof(PrepTaskType.BallDough), StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new SelectListItem("Ball Dough", nameof(PrepTaskType.BallDough), true));
        }

        return items;
    }

    private static IReadOnlyList<SelectListItem> BuildQuantityUnitOptions(string? taskType, string? selectedQuantityUnit)
    {
        var normalizedSelectedQuantityUnit = selectedQuantityUnit?.Trim() ?? nameof(Domain.Enums.DoughQuantityUnit.Balls);

        if (PrepTaskTypeExtensions.TryParse(taskType, out var parsedTaskType) && parsedTaskType == PrepTaskType.MakeDoughLoad)
        {
            return
            [
                new SelectListItem("Full Loads", nameof(Domain.Enums.DoughQuantityUnit.FullLoads), true)
            ];
        }

        if (PrepTaskTypeExtensions.TryParse(taskType, out parsedTaskType) && parsedTaskType == PrepTaskType.BallDough)
        {
            return
            [
                new SelectListItem("Dough Balls", nameof(Domain.Enums.DoughQuantityUnit.Balls), true)
            ];
        }

        return Enum.GetNames<Domain.Enums.DoughQuantityUnit>()
            .Select(unitName => new SelectListItem(
                unitName switch
                {
                    nameof(Domain.Enums.DoughQuantityUnit.Balls) => "Dough Balls",
                    nameof(Domain.Enums.DoughQuantityUnit.Cases) => "Cases",
                    nameof(Domain.Enums.DoughQuantityUnit.FullLoads) => "Full Loads",
                    _ => unitName
                },
                unitName,
                string.Equals(unitName, normalizedSelectedQuantityUnit, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private IReadOnlyList<string> GetAssignableRolesForManager()
    {
        if (User.IsInRole(nameof(ApplicationRole.Admin)))
        {
            return
            [
                nameof(ApplicationRole.Admin),
                nameof(ApplicationRole.Manager),
                nameof(ApplicationRole.PizzaMaker),
                nameof(ApplicationRole.Expo)
            ];
        }

        return
        [
            nameof(ApplicationRole.Manager),
            nameof(ApplicationRole.PizzaMaker),
            nameof(ApplicationRole.Expo)
        ];
    }

    private static bool IsFriendlyTaskException(Exception exception)
    {
        return exception is ArgumentException or
            ArgumentOutOfRangeException or
            InvalidOperationException or
            KeyNotFoundException;
    }

    private void SetStatusMessage(string statusType, string message)
    {
        TempData[StatusTypeKey] = statusType;
        TempData[StatusMessageKey] = message;
    }
}
