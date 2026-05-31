using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Domain.Rules;

public static class UserManagementRules
{
    private static readonly IReadOnlyList<ApplicationRole> AdminManageableRoles =
    [
        ApplicationRole.Admin,
        ApplicationRole.Manager,
        ApplicationRole.PizzaMaker,
        ApplicationRole.Expo
    ];

    private static readonly IReadOnlyList<ApplicationRole> ManagerManageableRoles =
    [
        ApplicationRole.PizzaMaker,
        ApplicationRole.Expo
    ];

    public static IReadOnlyList<ApplicationRole> GetManageableRoles(ApplicationRole actingRole)
    {
        return actingRole switch
        {
            ApplicationRole.Admin => AdminManageableRoles,
            ApplicationRole.Manager => ManagerManageableRoles,
            _ => Array.Empty<ApplicationRole>()
        };
    }

    public static bool CanManageRole(ApplicationRole actingRole, ApplicationRole targetRole)
    {
        return GetManageableRoles(actingRole).Contains(targetRole);
    }

    public static bool CanAssignRole(ApplicationRole actingRole, ApplicationRole targetRole)
    {
        return GetManageableRoles(actingRole).Contains(targetRole);
    }
}
