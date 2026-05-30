using System.Net;
using Microsoft.Extensions.Options;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Common;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Contracts.Responses.Auth;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Auth;

public sealed class PasswordService : IPasswordService
{
    private readonly FrontendOptions _frontendOptions;
    private readonly MailOptions _mailOptions;
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenService _userTokenService;
    private readonly ITemplateProvider _templateProvider;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;

    public PasswordService(
        IOptions<FrontendOptions> frontendOptions,
        IOptions<MailOptions> mailOptions,
        IUserRepository userRepository,
        IUserTokenService userTokenService,
        ITemplateProvider templateProvider,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork)
    {
        _frontendOptions = frontendOptions.Value;
        _mailOptions = mailOptions.Value;
        _userRepository = userRepository;
        _userTokenService = userTokenService;
        _templateProvider = templateProvider;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserResponse>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<UserResponse>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        var result = await _userRepository.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return ApiResponse<UserResponse>.Failure(
                "Password change failed.",
                HttpStatusCode.BadRequest,
                result.Errors.Select(static error => error.Description).ToArray());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<UserResponse>.Success(
            new UserResponse
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
            },
            "Password changed successfully.",
            HttpStatusCode.OK);
    }

    public async Task<ApiResponse<string>> SendPasswordResetEmailAsync(
        ResendEmailRequest request,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Success(
                request.Email,
                "If the email exists, a password reset link was sent.",
                HttpStatusCode.OK);
        }

        var tokenResponse = await _userTokenService.GeneratePasswordResetTokenAsync(user);
        if (!tokenResponse.IsSuccessful || string.IsNullOrWhiteSpace(tokenResponse.Result))
        {
            return ApiResponse<string>.Failure("Could not generate the reset token.", HttpStatusCode.BadRequest);
        }

        var template = await _templateProvider.GetTemplateAsync("PasswordReset", cancellationToken);
        var actionUrl = BuildUrl(
            ResolveBaseUrl(fallbackBaseUrl),
            _frontendOptions.ResetPasswordPath,
            new Dictionary<string, string?>
            {
                ["email"] = user.Email,
                ["token"] = tokenResponse.Result
            });

        var body = RenderTemplate(template, user, actionUrl);

        await _emailSender.SendAsync(
            user.FullName,
            user.Email!,
            _mailOptions.ResetPasswordSubject,
            body,
            cancellationToken);

        return ApiResponse<string>.Success(
            user.Email!,
            "If the email exists, a password reset link was sent.",
            HttpStatusCode.OK);
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return ApiResponse<bool>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        var result = await _userRepository.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return ApiResponse<bool>.Failure(
                "Password reset failed.",
                HttpStatusCode.BadRequest,
                result.Errors.Select(static error => error.Description).ToArray());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true, "Password reset successfully.", HttpStatusCode.OK);
    }

    private string ResolveBaseUrl(string fallbackBaseUrl)
    {
        return string.IsNullOrWhiteSpace(_frontendOptions.BaseUrl)
            ? fallbackBaseUrl.TrimEnd('/')
            : _frontendOptions.BaseUrl.TrimEnd('/');
    }

    private string RenderTemplate(string template, User user, string actionUrl)
    {
        var replacements = new Dictionary<string, string>
        {
            ["AppName"] = string.IsNullOrWhiteSpace(_mailOptions.BrandName) ? "Parlor Prediction" : _mailOptions.BrandName,
            ["UserName"] = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "there" : user.FullName,
            ["ActionUrl"] = actionUrl,
            ["SupportEmail"] = _mailOptions.FromAddress,
            ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
        };

        var renderedTemplate = template;
        foreach (var replacement in replacements)
        {
            renderedTemplate = renderedTemplate.Replace(
                $"{{{{{replacement.Key}}}}}",
                WebUtility.HtmlEncode(replacement.Value));
        }

        return renderedTemplate;
    }

    private static string BuildUrl(
        string baseUrl,
        string path,
        IReadOnlyDictionary<string, string?> queryParameters)
    {
        var normalizedPath = path.Trim().TrimStart('/');
        var query = string.Join(
            "&",
            queryParameters
                .Where(static item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item => $"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}"));

        var url = string.IsNullOrWhiteSpace(normalizedPath)
            ? baseUrl
            : $"{baseUrl}/{normalizedPath}";

        return string.IsNullOrWhiteSpace(query) ? url : $"{url}?{query}";
    }
}
