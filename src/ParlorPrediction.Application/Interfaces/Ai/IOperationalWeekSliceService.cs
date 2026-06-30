namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalWeekSliceService
{
    Task<OperationalWeekSliceResult> ExecuteAsync(
        OperationalWeekSliceRequest request,
        CancellationToken cancellationToken = default);
}
