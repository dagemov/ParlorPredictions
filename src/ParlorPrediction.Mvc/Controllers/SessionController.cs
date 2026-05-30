using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Contracts.Requests.Auth;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Mvc.Models.Session;

namespace ParlorPrediction.Mvc.Controllers;

[Route("session")]
public sealed class SessionController : Controller
{
    private readonly IAuthenticationService _authenticationService;
    private readonly SignInManager<User> _signInManager;

    public SessionController(
        IAuthenticationService authenticationService,
        SignInManager<User> signInManager)
    {
        _authenticationService = authenticationService;
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

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Prep");
    }
}
