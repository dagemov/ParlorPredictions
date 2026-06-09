using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.Session;

namespace ParlorPrediction.Mvc.Controllers;

[Route("session")]
public sealed class SessionController : Controller
{
    private const string SessionStatusTypeKey = "SessionStatusType";
    private const string SessionStatusMessageKey = "SessionStatusMessage";

    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly SignInManager<User> _signInManager;

    public SessionController(
        IAccountService accountService,
        IAuthenticationService authenticationService,
        IEmailConfirmationService emailConfirmationService,
        SignInManager<User> signInManager)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
        _emailConfirmationService = emailConfirmationService;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [AllowAnonymous]
    [HttpGet("register")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _accountService.RegisterAsync(
            new UserRegistrationRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                PasswordConfirmation = model.PasswordConfirmation,
                Role = nameof(ApplicationRole.Pending)
            },
            GetApplicationBaseUrl(),
            cancellationToken);

        if (!response.IsSuccessful)
        {
            ModelState.AddModelError(string.Empty, response.Message);

            foreach (var error in response.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        SetStatusMessage("success", response.Message);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _authenticationService.LoginAsync(
            new LoginRequest
            {
                Email = model.Email,
                Password = model.Password
            },
            cancellationToken);

        if (!response.IsSuccessful)
        {
            ModelState.AddModelError(string.Empty, response.Message);

            foreach (var error in response.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        string? userId,
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(new ConfirmEmailStatusViewModel
            {
                IsSuccessful = false,
                Title = "Confirmation link invalid",
                Message = "The confirmation link is invalid or expired."
            });
        }

        var response = await _emailConfirmationService.ConfirmEmailAsync(
            new ConfirmEmailRequest
            {
                UserId = userId,
                Token = token
            },
            cancellationToken);

        Response.StatusCode = (int)response.StatusCode;

        return View(new ConfirmEmailStatusViewModel
        {
            IsSuccessful = response.IsSuccessful,
            Title = response.IsSuccessful ? "Email confirmed successfully" : "Confirmation link invalid",
            Message = response.IsSuccessful
                ? "Your email was confirmed. An admin is pending to assign your role before you can access the system."
                : "The confirmation link is invalid or expired."
        });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private string GetApplicationBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
    }

    private void SetStatusMessage(string statusType, string message)
    {
        TempData[SessionStatusTypeKey] = statusType;
        TempData[SessionStatusMessageKey] = message;
    }
}
