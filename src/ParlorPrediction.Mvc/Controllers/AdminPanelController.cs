using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.AdminPanel;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("admin/panel")]
public sealed class AdminPanelController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["UseDoughExperience"] = true;

        return View(new AdminPanelPageViewModel
        {
            OperationsLinks =
            [
                new()
                {
                    Title = "Weekly Closing",
                    Description = "Close the operational week and carry real leftover dough into the next one.",
                    IconClass = "fa-solid fa-clipboard-check",
                    Controller = "WeeklyClosing",
                    Action = "Index"
                },
                new()
                {
                    Title = "Losses",
                    Description = "Review dough loss by date and reason without opening the kitchen workflow.",
                    IconClass = "fa-solid fa-chart-pie",
                    Controller = "DoughQuality",
                    Action = "Losses"
                },
                new()
                {
                    Title = "Advanced Dough Quality",
                    Description = "Use the full review, reball, discard, and correction tools.",
                    IconClass = "fa-solid fa-magnifying-glass-chart",
                    Controller = "DoughQuality",
                    Action = "Review"
                }
            ],
            DataLinks =
            [
                new()
                {
                    Title = "Prep Dough Data",
                    Description = "Manage demand plans and special events that affect dough forecasting.",
                    IconClass = "fa-solid fa-database",
                    Controller = "PrepData",
                    Action = "Index"
                },
                new()
                {
                    Title = "Restaurant Events",
                    Description = "Create or edit events that change dough demand for the week.",
                    IconClass = "fa-solid fa-calendar-check",
                    Controller = "PrepData",
                    Action = "Events"
                },
                new()
                {
                    Title = "Recommendations",
                    Description = "Capture manager overrides and recommendation notes outside the kitchen view.",
                    IconClass = "fa-solid fa-note-sticky",
                    Controller = "PrepRecommendations",
                    Action = "Create"
                }
            ],
            TeamLinks =
            [
                new()
                {
                    Title = "Users",
                    Description = "Manage approvals, roles, and active team members.",
                    IconClass = "fa-solid fa-users",
                    Controller = "AdminUsers",
                    Action = "Index"
                }
            ]
        });
    }
}
