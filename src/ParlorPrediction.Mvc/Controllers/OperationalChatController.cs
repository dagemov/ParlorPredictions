using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Application.Interfaces.Ai;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.OperationalChat;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("OperationalChat")]
public sealed class OperationalChatController : Controller
{
    private readonly IOperationalChatService _operationalChatService;

    public OperationalChatController(IOperationalChatService operationalChatService)
    {
        _operationalChatService = operationalChatService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["UseDoughExperience"] = true;

        return View(new OperationalChatInputViewModel
        {
            ReferenceDate = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [HttpPost("Send")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        OperationalChatInputViewModel input,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return PartialView(
                "_ChatMessage",
                OperationalChatViewModelMapper.MapValidationFailure(
                    input,
                    "Write the operational narrative before sending it to chat."));
        }

        try
        {
            var response = await _operationalChatService.SendAsync(
                new OperationalChatRequest
                {
                    SourceText = input.SourceText,
                    ReferenceDate = input.ReferenceDate,
                    TargetWeekStartDate = input.TargetWeekStartDate,
                    ActorUserId = currentUserId
                },
                cancellationToken);

            return PartialView(
                "_ChatMessage",
                OperationalChatViewModelMapper.MapTurn(input, response));
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            return PartialView(
                "_ChatMessage",
                OperationalChatViewModelMapper.MapValidationFailure(
                    input,
                    $"I could not safely interpret that message yet. {exception.Message}"));
        }
        catch
        {
            return PartialView(
                "_ChatMessage",
                OperationalChatViewModelMapper.MapValidationFailure(
                    input,
                    "I could not safely interpret that message yet. Please review the wording and try again."));
        }
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is ArgumentException
            or InvalidOperationException
            or KeyNotFoundException;
    }
}
