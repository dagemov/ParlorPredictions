namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalIntentClassifier
{
    Task<OperationalIntent> ClassifyAsync(
        string sourceText,
        DateOnly referenceDate,
        DateOnly? targetWeekStartDate = null,
        CancellationToken cancellationToken = default);
}
