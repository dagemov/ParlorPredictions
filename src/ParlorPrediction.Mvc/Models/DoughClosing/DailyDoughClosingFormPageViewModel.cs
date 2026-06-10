namespace ParlorPrediction.Mvc.Models.DoughClosing;

public sealed class DailyDoughClosingFormPageViewModel
{
    public string Title { get; set; } = "Daily Dough Closing";

    public string Intro { get; set; } = string.Empty;

    public DailyDoughClosingFormViewModel Form { get; set; } = new();
}
