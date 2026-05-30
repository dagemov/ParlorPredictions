using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Contracts.Requests.Auth;

namespace ParlorPrediction.Mvc.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly IPasswordService _passwordService;

    public AccountsController(
        IAccountService accountService,
        IAuthenticationService authenticationService,
        IEmailConfirmationService emailConfirmationService,
        IPasswordService passwordService)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
        _emailConfirmationService = emailConfirmationService;
        _passwordService = passwordService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequest request, CancellationToken cancellationToken)
    {
        var response = await _accountService.RegisterAsync(request, GetApplicationBaseUrl(), cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authenticationService.LoginAsync(request, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authenticationService.RefreshTokenAsync(request, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await _accountService.GetCurrentUserAsync(userId, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPut("profile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await _accountService.UpdateAsync(userId, request, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var response = await _emailConfirmationService.ConfirmEmailAsync(
            new ConfirmEmailRequest
            {
                UserId = userId,
                Token = token
            },
            cancellationToken);

        return response.IsSuccessful
            ? Content("Email confirmed successfully. You can return to Parlor Prediction and sign in.", "text/plain")
            : StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var response = await _emailConfirmationService.ConfirmEmailAsync(request, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailRequest request, CancellationToken cancellationToken)
    {
        var response = await _emailConfirmationService.ResendConfirmationEmailAsync(request, GetApplicationBaseUrl(), cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var response = await _passwordService.ChangePasswordAsync(userId, request, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("request-password-reset")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset([FromBody] ResendEmailRequest request, CancellationToken cancellationToken)
    {
        var response = await _passwordService.SendPasswordResetEmailAsync(request, GetApplicationBaseUrl(), cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await _passwordService.ResetPasswordAsync(request, cancellationToken);
        return StatusCode((int)response.StatusCode, response);
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private string GetApplicationBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
    }
}
