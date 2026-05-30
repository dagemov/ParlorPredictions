namespace ParlorPrediction.Application.Configuration;

public sealed class FrontendOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ConfirmEmailPath { get; set; } = "api/accounts/confirm-email";

    public string ResetPasswordPath { get; set; } = "reset-password";
}
