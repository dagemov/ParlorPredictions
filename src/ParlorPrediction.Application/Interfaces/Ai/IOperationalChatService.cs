namespace ParlorPrediction.Application.Interfaces.Ai;

public interface IOperationalChatService
{
    Task<OperationalChatResponse> SendAsync(
        OperationalChatRequest request,
        CancellationToken cancellationToken = default);
}
