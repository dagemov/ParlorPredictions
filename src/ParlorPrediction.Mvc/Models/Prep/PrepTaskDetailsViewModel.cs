namespace ParlorPrediction.Mvc.Models.Prep;

public sealed class PrepTaskDetailsViewModel
{
    public DoughTaskViewModel Task { get; set; } = new();

    public bool CanManageTasks { get; set; }
}
