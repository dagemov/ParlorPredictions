namespace ParlorPrediction.Mvc.Models.AdminPanel;

public sealed class AdminPanelLinkViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string IconClass { get; set; } = string.Empty;

    public string Controller { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? RouteName { get; set; }

    public string? RouteValue { get; set; }
}
