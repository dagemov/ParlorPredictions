namespace ParlorPrediction.Application.Configuration;

public sealed class MailOptions
{
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string ConfirmationSubject { get; set; } = string.Empty;

    public string ResetPasswordSubject { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Password { get; set; } = string.Empty;
}
