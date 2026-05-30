using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Mappings;

public static class UserContractMappings
{
    public static User ToUser(this UserRegistrationRequest request, ApplicationRole role)
    {
        return new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email.Trim() : request.UserName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            ProfileImageUrl = request.ProfileImageUrl,
            Role = role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public static void Apply(this UserUpdateRequest request, User user)
    {
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.ProfileImageUrl = request.ProfileImageUrl;
    }

    public static UserResponse ToUserResponse(this User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.GetCanonicalName(),
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }
}
