namespace ParlorPrediction.Application.Configuration;

public sealed class DevelopmentSeedUserOptions
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
