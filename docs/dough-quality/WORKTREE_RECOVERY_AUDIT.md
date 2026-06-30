# Worktree Recovery Audit

## Unauthorized Parallel Folder Detected

- Detected folder: `C:\Users\Hombr\source\repos\ParlorPredictions-dough-used-today`
- Official repository and only source of truth:
  `C:\Users\Hombr\source\repos\ParlorPredictions`

## Why The Parallel Folder Must Not Be Used

- It is a separate checkout path outside the official working directory.
- It created a branch/worktree ownership conflict for `feature/dough-used-today-reball-planning`.
- It increases the risk of code drifting away from the official repo history.
- It makes selective staging, commit intent, and PR review harder to control.

## Useful Files Found In The Parallel Folder

### Docs

- `docs/dough-quality/DAILY_DOUGH_USAGE_RULES.md`
- `docs/dough-quality/DOUGH_SOURCE_USAGE_AND_REBALL_REQUIREMENTS.md`

### Domain / Rules / Contracts

- `src/ParlorPrediction.Domain/Entities/DoughUsageTrace.cs`
- `src/ParlorPrediction.Domain/Rules/DoughRules.cs`
- `src/ParlorPrediction.Contracts/Requests/DoughUsage/CreateDoughUsageTraceRequest.cs`
- `src/ParlorPrediction.Contracts/Requests/DoughUsage/CorrectDoughUsageTraceRequest.cs`
- `src/ParlorPrediction.Contracts/Responses/DoughUsage/DoughUsageTraceResponse.cs`
- `src/ParlorPrediction.Contracts/Responses/DoughClosing/DailyClosingOperationalInsightsResponse.cs`

### Application

- `src/ParlorPrediction.Application/Services/Dough/DoughAvailabilityProjectionService.cs`
- `src/ParlorPrediction.Application/Services/Dough/DailyDoughClosingReadService.cs`
- `src/ParlorPrediction.Application/Services/Dough/DoughUsageTraceManagementService.cs`

### MVC

- `src/ParlorPrediction.Mvc/Controllers/DoughUsageController.cs`
- `src/ParlorPrediction.Mvc/Controllers/DailyClosingController.cs`
- `src/ParlorPrediction.Mvc/Controllers/PrepController.cs`
- `src/ParlorPrediction.Mvc/Controllers/HomeController.cs`
- `src/ParlorPrediction.Mvc/Models/DoughClosing/DailyDoughClosingIndexViewModel.cs`
- `src/ParlorPrediction.Mvc/Models/DoughUsage/DoughUsageSourceCardViewModel.cs`
- `src/ParlorPrediction.Mvc/Models/DoughUsage/DoughUsageTraceFormViewModel.cs`
- `src/ParlorPrediction.Mvc/Models/DoughUsage/DoughUsageTraceListItemViewModel.cs`
- `src/ParlorPrediction.Mvc/Models/Home/DailyClosingOperationalInsightsViewModel.cs`
- `src/ParlorPrediction.Mvc/Views/DailyClosing/Index.cshtml`
- `src/ParlorPrediction.Mvc/Views/DoughUsage/Create.cshtml`
- `src/ParlorPrediction.Mvc/Views/Home/Index.cshtml`
- `src/ParlorPrediction.Mvc/Views/Prep/_DoughProductionPlanningPartial.cshtml`

### Persistence / EF

- `src/ParlorPrediction.Persistence/Configurations/DoughUsageTraceConfiguration.cs`
- `src/ParlorPrediction.Persistence/Migrations/20260617193329_AllowFractionalDoughUsageTraces.cs`
- `src/ParlorPrediction.Persistence/Migrations/20260617193329_AllowFractionalDoughUsageTraces.Designer.cs`
- `src/ParlorPrediction.Persistence/Migrations/ParlorPredictionDbContextModelSnapshot.cs`

### Tests

- `tests/ParlorPrediction.Application.Tests/DailyDoughClosingServicesTests.cs`
- `tests/ParlorPrediction.Application.Tests/DoughAvailabilityProjectionServiceTests.cs`
- `tests/ParlorPrediction.Application.Tests/DoughRegistrationFlowTests.cs`
- `tests/ParlorPrediction.Application.Tests/DoughUsageTraceServicesTests.cs`
- `tests/ParlorPrediction.Application.Tests/PrepWeeklyDoughCalendarServiceTests.cs`

## Files Explicitly Discarded From Recovery

- `src/ParlorPrediction.Persistence/ParlorPredictionDbContext.cs`
  Reason: local modified state already existed in the official repo and is unrelated to this recovery.
- `src/ParlorPrediction.Mvc/Properties/serviceDependencies.local.json`
  Reason: local machine file, untracked, not part of the feature.
- Any `bin/`, `obj/`, temp, generated local artifacts, or hidden editor state.

## Controlled Port Plan

1. Work only inside `C:\Users\Hombr\source\repos\ParlorPredictions`.
2. Keep `feature/dough-used-today-reball-planning` as the official recovery branch.
3. Port only reviewed diffs from the parallel folder.
4. Stage and commit by intent:
   - audit doc
   - rules docs
   - fractional trace support
   - ready dough calculation fix
   - dough usage UI
   - daily closing reconciliation warning
   - tests
5. Run `dotnet build` and `dotnet test` in the official repo before any PR.
6. Do not delete the parallel folder unless the user authorizes that cleanup after verification.

## Risks

- The official repo currently contains unrelated local state that must not be staged accidentally.
- The fractional trace migration changes a persisted column type and must be reviewed before database update.
- MVC mapping files and view models must stay aligned with the new reconciliation fields.
- Recovery must preserve the rule that closed-day usage is owned by `Daily Closing`, while open-day traces only affect live availability.
