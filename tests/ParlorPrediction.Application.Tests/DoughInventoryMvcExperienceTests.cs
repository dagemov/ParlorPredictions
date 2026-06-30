using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Mvc.Controllers;
using ParlorPrediction.Mvc.Models.DoughInventory;
using Xunit;

namespace ParlorPrediction.Application.Tests;

public sealed class DoughInventoryMvcExperienceTests
{
    [Fact]
    public void DoughInventoryViewModelMapper_KeepsHomePreviewAndFullPageTotalsAligned()
    {
        var response = new DoughInventoryImpactResponse
        {
            ReferenceDate = new DateOnly(2026, 6, 17),
            WeekStartDate = new DateOnly(2026, 6, 16),
            WeekEndDate = new DateOnly(2026, 6, 21),
            WeeklyGoalBalls = 943,
            ReadyNowBalls = 720,
            StillMissingBalls = 223,
            UsedTodayBalls = 36,
            UseFirstBalls = 48,
            AttentionBalls = 24,
            MixedButNotBalledBalls = 168,
            FutureBalls = 168,
            LostOrDiscardedBalls = 12,
            RemainingTrackedBalls = 792,
            RemainingSources =
            [
                new DoughInventoryImpactSourceResponse
                {
                    SourceDoughBatchQualityRecordId = Guid.NewGuid(),
                    SourceDate = new DateOnly(2026, 6, 14),
                    CreatedOrBalledAt = new DateTime(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc),
                    SourceType = "MustUseNextDay",
                    RemainingBalls = 48,
                    OriginalBalls = 48,
                    CountsAsAvailable = true,
                    RecommendedAction = "UseFirst"
                }
            ]
        };

        var summary = DoughInventoryViewModelMapper.MapSummary(response);
        var page = DoughInventoryViewModelMapper.MapPage(response, historicalWeeksToUse: 8);

        Assert.Equal(summary.ReadyNowBalls, page.Summary.ReadyNowBalls);
        Assert.Equal(summary.StillMissingBalls, page.Summary.StillMissingBalls);
        Assert.Equal(summary.UseFirstBalls, page.Summary.UseFirstBalls);
        Assert.Equal(summary.MixedButNotBalledBalls, page.Summary.MixedButNotBalledBalls);
        Assert.Equal(summary.UsedTodayBalls, page.Summary.UsedTodayBalls);
        Assert.Equal(summary.LostOrDiscardedBalls, page.Summary.LostOrDiscardedBalls);
    }

    [Fact]
    public void Layout_KeepsSingleSidebarNav_AndAddsDoughInventoryOnlyOnce()
    {
        var layoutPath = Path.Combine(GetRepositoryRoot(), "src", "ParlorPrediction.Mvc", "Views", "Shared", "_Layout.cshtml");
        var markup = File.ReadAllText(layoutPath);

        Assert.Single(Regex.Matches(markup, "pp-sidebar__nav").Cast<Match>());
        Assert.Single(Regex.Matches(markup, "Dough Inventory").Cast<Match>());
    }

    [Fact]
    public void SessionAndUsersControllers_KeepLoginAndUserAccessRules()
    {
        AssertAllowAnonymous(typeof(SessionController).GetMethod(nameof(SessionController.Login), [typeof(string)]));
        AssertAllowAnonymous(typeof(SessionController).GetMethod(nameof(SessionController.Register), Type.EmptyTypes));
        AssertAllowAnonymous(typeof(SessionController).GetMethod(nameof(SessionController.Login), [typeof(ParlorPrediction.Mvc.Models.Session.LoginViewModel), typeof(CancellationToken)]));
        AssertAllowAnonymous(typeof(SessionController).GetMethod(nameof(SessionController.Register), [typeof(ParlorPrediction.Mvc.Models.Session.RegisterViewModel), typeof(CancellationToken)]));

        var authorize = Assert.Single(typeof(AdminUsersController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Contains("Admin", authorize.Roles ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Manager", authorize.Roles ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertAllowAnonymous(MethodInfo? method)
    {
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(inherit: true), attribute => attribute is AllowAnonymousAttribute);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ParlorPrediction.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }
}
