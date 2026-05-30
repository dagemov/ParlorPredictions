using System.Net;
using Microsoft.Extensions.Options;
using ParlorPrediction.Application.Configuration;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Common;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Common;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Auth;

public sealed class EmailConfirmationService : IEmailConfirmationService
{
    private readonly FrontendOptions _frontendOptions;
    private readonly MailOptions _mailOptions;
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenService _userTokenService;
    private readonly ITemplateProvider _templateProvider;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;

    public EmailConfirmationService(
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

    public async Task<ApiResponse<bool>> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<bool>.Failure("User not found.", HttpStatusCode.NotFound);
        }

        var result = await _userRepository.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return ApiResponse<bool>.Failure(
                "Email confirmation failed.",
                HttpStatusCode.BadRequest,
                result.Errors.Select(static error => error.Description).ToArray());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true, "Email confirmed successfully.", HttpStatusCode.OK);
    }

    public async Task<ApiResponse<string>> SendConfirmationEmailAsync(
        User user,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var tokenResponse = await _userTokenService.GenerateEmailConfirmationTokenAsync(user);
        if (!tokenResponse.IsSuccessful || string.IsNullOrWhiteSpace(tokenResponse.Result))
        {
            return ApiResponse<string>.Failure("Could not generate the confirmation token.", HttpStatusCode.BadRequest);
        }

        var template = await _templateProvider.GetTemplateAsync("EmailConfirmation", cancellationToken);
        var actionUrl = BuildUrl(
            ResolveBaseUrl(fallbackBaseUrl),
            string.IsNullOrWhiteSpace(_frontendOptions.ConfirmEmailPath) ? "api/accounts/confirm-email" : _frontendOptions.ConfirmEmailPath,
            new Dictionary<string, string?>
            {
                ["userId"] = user.Id,
                ["token"] = tokenResponse.Result
            });

        var body = RenderTemplate(template, user, actionUrl);

        await _emailSender.SendAsync(
            user.FullName,
            user.Email!,
            _mailOptions.ConfirmationSubject,
            body,
            cancellationToken);

        return ApiResponse<string>.Success(user.Email, "Confirmation email sent.", HttpStatusCode.OK);
    }

    public async Task<ApiResponse<string>> ResendConfirmationEmailAsync(
        ResendEmailRequest request,
        string fallbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Success(
                request.Email,
                "If we found an account with that email, we sent a fresh confirmation link.",
                HttpStatusCode.OK);
        }

        if (user.EmailConfirmed)
        {
            return ApiResponse<string>.Success(user.Email!, "That account is already confirmed.", HttpStatusCode.OK);
        }

        var response = await SendConfirmationEmailAsync(user, fallbackBaseUrl, cancellationToken);
        return response.IsSuccessful
            ? ApiResponse<string>.Success(user.Email!, "If we found an account with that email, we sent a fresh confirmation link.", HttpStatusCode.OK)
            : response;
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
