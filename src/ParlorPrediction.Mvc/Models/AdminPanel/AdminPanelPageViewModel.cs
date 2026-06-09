namespace ParlorPrediction.Mvc.Models.AdminPanel;

public sealed class AdminPanelPageViewModel
{
    public IReadOnlyList<AdminPanelLinkViewModel> OperationsLinks { get; set; } = Array.Empty<AdminPanelLinkViewModel>();

    public IReadOnlyList<AdminPanelLinkViewModel> DataLinks { get; set; } = Array.Empty<AdminPanelLinkViewModel>();

    public IReadOnlyList<AdminPanelLinkViewModel> TeamLinks { get; set; } = Array.Empty<AdminPanelLinkViewModel>();
}
