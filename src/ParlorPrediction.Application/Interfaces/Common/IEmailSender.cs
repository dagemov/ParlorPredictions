namespace ParlorPrediction.Application.Interfaces.Common;

public interface IEmailSender
{
    Task SendAsync(
        string toName,
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
