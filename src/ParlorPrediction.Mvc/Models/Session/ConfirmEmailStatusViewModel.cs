namespace ParlorPrediction.Mvc.Models.Session;

public sealed class ConfirmEmailStatusViewModel
{
    public bool IsSuccessful { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
