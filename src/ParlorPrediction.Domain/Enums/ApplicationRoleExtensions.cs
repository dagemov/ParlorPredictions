namespace ParlorPrediction.Domain.Enums;

public static class ApplicationRoleExtensions
{
    public static string GetCanonicalName(this ApplicationRole role)
    {
        return role switch
        {
            ApplicationRole.Pending => nameof(ApplicationRole.Pending),
            ApplicationRole.Admin => nameof(ApplicationRole.Admin),
            ApplicationRole.Manager => nameof(ApplicationRole.Manager),
            ApplicationRole.PizzaMaker => nameof(ApplicationRole.PizzaMaker),
            ApplicationRole.Expo => nameof(ApplicationRole.Expo),
            _ => role.ToString()
        };
    }

    public static string Normalize(string? roleName)
    {
        var normalized = roleName?.Trim() ?? string.Empty;

        return normalized.ToLowerInvariant() switch
        {
            "pending" => nameof(ApplicationRole.Pending),
            "admin" => nameof(ApplicationRole.Admin),
            "manager" => nameof(ApplicationRole.Manager),
            "pizzamaker" or "pizza-maker" or "pizza_maker" or "pizza maker" => nameof(ApplicationRole.PizzaMaker),
            "expo" => nameof(ApplicationRole.Expo),
            _ => normalized
        };
    }

    public static bool TryParse(string? roleName, out ApplicationRole role)
    {
        return Enum.TryParse(Normalize(roleName), true, out role);
    }

    public static IReadOnlyCollection<string> GetAllNames()
    {
        return
        [
            nameof(ApplicationRole.Pending),
            nameof(ApplicationRole.Admin),
            nameof(ApplicationRole.Manager),
            nameof(ApplicationRole.PizzaMaker),
            nameof(ApplicationRole.Expo)
        ];
    }
}
