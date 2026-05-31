using System.ComponentModel.DataAnnotations;

namespace ParlorPrediction.Contracts.Requests.Users;

public sealed class ChangeManagedUserRoleRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
