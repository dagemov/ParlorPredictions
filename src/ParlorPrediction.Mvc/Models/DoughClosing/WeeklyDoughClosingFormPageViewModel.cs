namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class WeeklyDoughClosingFormPageViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Intro { get; set; } = string.Empty;

    public WeeklyDoughClosingFormViewModel Form { get; set; } = new();
}
