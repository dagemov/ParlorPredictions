using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Common;

namespace ParlorPrediction.Infrastructure.Services.Mail;

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly MailOptions _mailOptions;

    public MailKitEmailSender(IOptions<MailOptions> mailOptions)
    {
        _mailOptions = mailOptions.Value;
    }

    public async Task SendAsync(
        string toName,
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var fromAddress = _mailOptions.GetFromAddress();
        var smtpHost = _mailOptions.GetSmtpHost();
        var smtpUsername = _mailOptions.GetUsername();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_mailOptions.FromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        await client.ConnectAsync(smtpHost, _mailOptions.Port, SecureSocketOptions.StartTls, cancellationToken);
        client.AuthenticationMechanisms.Remove("XOAUTH2");
        await client.AuthenticateAsync(smtpUsername, _mailOptions.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
