using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Contracts.Requests.Users;
using ParlorPrediction.Contracts.Responses.Users;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;
using ParlorPrediction.Mvc.Models.AdminUsers;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("admin/users")]
public sealed class AdminUsersController : Controller
{
    private const string StatusTypeKey = "AdminUsersStatusType";
    private const string StatusMessageKey = "AdminUsersStatusMessage";

    private readonly IUserManagementService _userManagementService;

    public AdminUsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? term,
        string? role,
        bool activeOnly = true,
        bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        if (pendingOnly)
        {
            activeOnly = false;
        }

        var users = await _userManagementService.SearchAsync(
            new SearchUsersRequest
            {
                Term = term,
                Role = role,
                ActiveOnly = activeOnly,
                PendingOnly = pendingOnly
            },
            actingRole,
            cancellationToken);

        var model = new AdminUserListPageViewModel
        {
            Term = term,
            Role = role,
            ActiveOnly = activeOnly,
            PendingOnly = pendingOnly,
            RoleOptions = BuildManageableRoleOptions(actingRole, role, includeAllOption: true),
            Users = users.Select(MapListItem).ToArray(),
            StatusType = ReadStatusType(),
            StatusMessage = ReadStatusMessage()
        };

        if (IsHtmxRequest())
        {
            return PartialView("_UserListPartial", model);
        }

        return View(model);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        var actingRole = GetActingRole();
        return View(BuildCreateViewModel(new AdminUserCreateViewModel(), actingRole));
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminUserCreateViewModel model,
        CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        if (!ModelState.IsValid)
        {
            return View(BuildCreateViewModel(model, actingRole));
        }

        var response = await _userManagementService.CreateAsync(
            new CreateManagedUserRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                Password = model.Password,
                PasswordConfirmation = model.PasswordConfirmation
            },
            actingRole,
            GetApplicationBaseUrl(),
            cancellationToken);

        if (!response.IsSuccessful || response.Result is null)
        {
            AddErrorsToModelState(response);
            return View(BuildCreateViewModel(model, actingRole));
        }

        SetStatusMessage("success", response.Message);
        return RedirectToAction(nameof(Details), new { id = response.Result.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        var user = await _userManagementService.GetByIdAsync(id, actingRole, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return View(MapDetails(user, ReadStatusType(), ReadStatusMessage()));
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        var user = await _userManagementService.GetByIdAsync(id, actingRole, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return View(new AdminUserEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CurrentProfileImageUrl = user.ProfileImageUrl
        });
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        AdminUserEditViewModel model,
        CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        model.Id = id;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _userManagementService.UpdateAsync(
            id,
            new UpdateManagedUserRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            },
            actingRole,
            cancellationToken);

        if (!response.IsSuccessful || response.Result is null)
        {
            AddErrorsToModelState(response);
            return View(model);
        }

        SetStatusMessage("success", response.Message);
        return RedirectToAction(nameof(Details), new { id = response.Result.Id });
    }

    [HttpGet("{id}/roles")]
    public async Task<IActionResult> Roles(string id, CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        var user = await _userManagementService.GetByIdAsync(id, actingRole, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return View(BuildRoleViewModel(user, actingRole));
    }

    [HttpPost("{id}/roles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Roles(
        string id,
        UserRoleManagementViewModel model,
        CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        model.Id = id;

        if (!ModelState.IsValid)
        {
            var currentUser = await _userManagementService.GetByIdAsync(id, actingRole, cancellationToken);
            if (currentUser is null)
            {
                return NotFound();
            }

            return View(BuildRoleViewModel(currentUser, actingRole, model.Role));
        }

        var response = await _userManagementService.ChangeRoleAsync(
            id,
            new ChangeManagedUserRoleRequest
            {
                Role = model.Role
            },
            actingRole,
            GetCurrentUserId(),
            cancellationToken);

        if (!response.IsSuccessful || response.Result is null)
        {
            AddErrorsToModelState(response);
            var currentUser = await _userManagementService.GetByIdAsync(id, actingRole, cancellationToken);
            if (currentUser is null)
            {
                return NotFound();
            }

            return View(BuildRoleViewModel(currentUser, actingRole, model.Role));
        }

        SetStatusMessage("success", response.Message);
        return RedirectToAction(nameof(Details), new { id = response.Result.Id });
    }

    [HttpPost("{id}/toggle-active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(
        string id,
        string? term,
        string? role,
        bool activeOnly = true,
        bool pendingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var actingRole = GetActingRole();
        var currentUser = await _userManagementService.GetByIdAsync(id, actingRole, cancellationToken);
        if (currentUser is null)
        {
            return NotFound();
        }

        var response = await _userManagementService.SetActiveAsync(
            id,
            !currentUser.IsActive,
            actingRole,
            GetCurrentUserId(),
            cancellationToken);

        if (IsHtmxRequest())
        {
            TriggerHtmxAlert(
                response.IsSuccessful ? "success" : "error",
                response.IsSuccessful ? "User updated" : "User update failed",
                response.Message);

            return await Index(term, role, activeOnly, pendingOnly, cancellationToken);
        }

        SetStatusMessage(response.IsSuccessful ? "success" : "danger", response.Message);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id}/resend-confirmation")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(
        string id,
        CancellationToken cancellationToken = default)
    {
        var response = await _userManagementService.ResendConfirmationAsync(
            id,
            GetActingRole(),
            GetApplicationBaseUrl(),
            cancellationToken);

        SetStatusMessage(response.IsSuccessful ? "success" : "danger", response.Message);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id}/photo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhoto(
        string id,
        IFormFile profilePhoto,
        CancellationToken cancellationToken = default)
    {
        if (profilePhoto is null || profilePhoto.Length == 0)
        {
            SetStatusMessage("danger", "Choose an image before uploading a profile photo.");
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var memoryStream = new MemoryStream();
        await profilePhoto.CopyToAsync(memoryStream, cancellationToken);

        var response = await _userManagementService.UpdateProfilePhotoAsync(
            id,
            memoryStream.ToArray(),
            Path.GetExtension(profilePhoto.FileName),
            profilePhoto.FileName,
            profilePhoto.ContentType,
            GetActingRole(),
            cancellationToken);

        SetStatusMessage(response.IsSuccessful ? "success" : "danger", response.Message);
        return RedirectToAction(nameof(Details), new { id });
    }

    private AdminUserCreateViewModel BuildCreateViewModel(AdminUserCreateViewModel model, ApplicationRole actingRole)
    {
        model.RoleOptions = BuildAssignableRoleOptions(actingRole, model.Role, includeAllOption: false);
        return model;
    }

    private UserRoleManagementViewModel BuildRoleViewModel(
        ManagedUserDetailResponse user,
        ApplicationRole actingRole,
        string? selectedRole = null)
    {
        return new UserRoleManagementViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            CurrentRole = user.Role,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            ProfileImageUrl = user.ProfileImageUrl,
            Role = string.IsNullOrWhiteSpace(selectedRole) ? user.Role : selectedRole,
            RoleOptions = BuildAssignableRoleOptions(actingRole, string.IsNullOrWhiteSpace(selectedRole) ? user.Role : selectedRole, includeAllOption: false)
        };
    }

    private static AdminUserListItemViewModel MapListItem(ManagedUserListItemResponse response)
    {
        return new AdminUserListItemViewModel
        {
            Id = response.Id,
            UserName = response.UserName,
            FullName = response.FullName,
            Email = response.Email,
            PhoneNumber = response.PhoneNumber,
            Role = response.Role,
            IsActive = response.IsActive,
            EmailConfirmed = response.EmailConfirmed,
            ProfileImageUrl = response.ProfileImageUrl,
            UpdatedAtUtc = response.UpdatedAtUtc
        };
    }

    private static AdminUserDetailsViewModel MapDetails(
        ManagedUserDetailResponse response,
        string? statusType,
        string? statusMessage)
    {
        return new AdminUserDetailsViewModel
        {
            Id = response.Id,
            UserName = response.UserName,
            FirstName = response.FirstName,
            LastName = response.LastName,
            FullName = response.FullName,
            Email = response.Email,
            PhoneNumber = response.PhoneNumber,
            Role = response.Role,
            IsActive = response.IsActive,
            EmailConfirmed = response.EmailConfirmed,
            ProfileImageUrl = response.ProfileImageUrl,
            CreatedAtUtc = response.CreatedAtUtc,
            UpdatedAtUtc = response.UpdatedAtUtc,
            StatusType = statusType,
            StatusMessage = statusMessage
        };
    }

    private IReadOnlyList<SelectListItem> BuildManageableRoleOptions(
        ApplicationRole actingRole,
        string? selectedRole,
        bool includeAllOption)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("All roles", string.Empty, string.IsNullOrWhiteSpace(selectedRole)));
        }

        items.AddRange(
            UserManagementRules.GetManageableRoles(actingRole)
                .Select(role => new SelectListItem(
                    role.GetCanonicalName(),
                    role.GetCanonicalName(),
                    string.Equals(role.GetCanonicalName(), selectedRole, StringComparison.OrdinalIgnoreCase))));

        return items;
    }

    private IReadOnlyList<SelectListItem> BuildAssignableRoleOptions(
        ApplicationRole actingRole,
        string? selectedRole,
        bool includeAllOption)
    {
        var items = new List<SelectListItem>();
        if (includeAllOption)
        {
            items.Add(new SelectListItem("All roles", string.Empty, string.IsNullOrWhiteSpace(selectedRole)));
        }

        items.AddRange(
            UserManagementRules.GetAssignableRoles(actingRole)
                .Select(role => new SelectListItem(
                    role.GetCanonicalName(),
                    role.GetCanonicalName(),
                    string.Equals(role.GetCanonicalName(), selectedRole, StringComparison.OrdinalIgnoreCase))));

        return items;
    }

    private ApplicationRole GetActingRole()
    {
        var roleName = User.FindFirstValue(ClaimTypes.Role);
        if (ApplicationRoleExtensions.TryParse(roleName, out var role))
        {
            return role;
        }

        throw new InvalidOperationException("The current user role could not be resolved.");
    }

    private string GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("The current user id could not be resolved.");
        }

        return userId;
    }

    private string GetApplicationBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
    }

    private bool IsHtmxRequest()
    {
        return Request.Headers.TryGetValue("HX-Request", out var headerValue) &&
            string.Equals(headerValue.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private void TriggerHtmxAlert(string type, string title, string message)
    {
        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new
        {
            prepAlert = new
            {
                type,
                title,
                message
            }
        });
    }

    private void SetStatusMessage(string statusType, string message)
    {
        TempData[StatusTypeKey] = statusType;
        TempData[StatusMessageKey] = message;
    }

    private string? ReadStatusType()
    {
        return TempData[StatusTypeKey] as string;
    }

    private string? ReadStatusMessage()
    {
        return TempData[StatusMessageKey] as string;
    }

    private void AddErrorsToModelState<T>(ParlorPrediction.Contracts.Common.ApiResponse<T> response)
    {
        ModelState.AddModelError(string.Empty, response.Message);

        foreach (var error in response.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}
