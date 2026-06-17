using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Dough;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Mvc.Models.AdminDoughCorrections;

namespace ParlorPrediction.Mvc.Controllers;

[Authorize(Roles = $"{nameof(ApplicationRole.Admin)},{nameof(ApplicationRole.Manager)}")]
[Route("admin/dough-corrections")]
public sealed class AdminDoughCorrectionsController : Controller
{
    private const string StatusTypeKey = "AdminDoughCorrectionsStatusType";
    private const string StatusMessageKey = "AdminDoughCorrectionsStatusMessage";

    private readonly IConfiguration _configuration;
    private readonly IDailyDoughClosingReadService _dailyDoughClosingReadService;
    private readonly IDoughAvailabilityProjectionService _doughAvailabilityProjectionService;
    private readonly IDoughBatchReadRepository _doughBatchReadRepository;
    private readonly IDoughCorrectionAdminService _doughCorrectionAdminService;
    private readonly IDoughProductionPlanningService _doughProductionPlanningService;
    private readonly IDoughQualityReadService _doughQualityReadService;
    private readonly IDoughUsageTraceReadService _doughUsageTraceReadService;
    private readonly IPrepTaskReadService _prepTaskReadService;
    private readonly IPrepWeeklyDoughCalendarService _prepWeeklyDoughCalendarService;
    private readonly IWeeklyDoughClosingReadService _weeklyDoughClosingReadService;

    public AdminDoughCorrectionsController(
        IConfiguration configuration,
        IDailyDoughClosingReadService dailyDoughClosingReadService,
        IDoughAvailabilityProjectionService doughAvailabilityProjectionService,
        IDoughBatchReadRepository doughBatchReadRepository,
        IDoughCorrectionAdminService doughCorrectionAdminService,
        IDoughProductionPlanningService doughProductionPlanningService,
        IDoughQualityReadService doughQualityReadService,
        IDoughUsageTraceReadService doughUsageTraceReadService,
        IPrepTaskReadService prepTaskReadService,
        IPrepWeeklyDoughCalendarService prepWeeklyDoughCalendarService,
        IWeeklyDoughClosingReadService weeklyDoughClosingReadService)
    {
        _configuration = configuration;
        _dailyDoughClosingReadService = dailyDoughClosingReadService;
        _doughAvailabilityProjectionService = doughAvailabilityProjectionService;
        _doughBatchReadRepository = doughBatchReadRepository;
        _doughCorrectionAdminService = doughCorrectionAdminService;
        _doughProductionPlanningService = doughProductionPlanningService;
        _doughQualityReadService = doughQualityReadService;
        _doughUsageTraceReadService = doughUsageTraceReadService;
        _prepTaskReadService = prepTaskReadService;
        _prepWeeklyDoughCalendarService = prepWeeklyDoughCalendarService;
        _weeklyDoughClosingReadService = weeklyDoughClosingReadService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        DateOnly? referenceDate,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;
        var selectedDate = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);

        return View(await BuildIndexViewModelAsync(selectedDate, cancellationToken));
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)}")]
    [HttpGet("prep-tasks/{id:guid}/edit")]
    public async Task<IActionResult> EditPrepTask(
        Guid id,
        DateOnly? referenceDate,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var task = await _prepTaskReadService.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            SetStatusMessage("danger", "The requested prep task could not be found.");
            return RedirectToAction(nameof(Index), new { referenceDate });
        }

        return View("EditPrepTask", BuildPrepTaskForm(task, referenceDate ?? task.TaskDate));
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)}")]
    [HttpPost("prep-tasks/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPrepTask(
        Guid id,
        AdminPrepTaskCorrectionFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;
        model.PrepTaskId = id;

        if (string.Equals(model.Status, nameof(PrepTaskStatus.Completed), StringComparison.OrdinalIgnoreCase) &&
            model.QuantityCompleted <= 0)
        {
            ModelState.AddModelError(nameof(model.QuantityCompleted), "Completed tasks must keep a completed quantity greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            ApplyPrepTaskOptions(model);
            return View("EditPrepTask", model);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _doughCorrectionAdminService.CorrectPrepTaskAsync(
                new AdminCorrectPrepTaskRequest
                {
                    PrepTaskId = id,
                    TaskDate = model.TaskDate,
                    TaskType = model.TaskType,
                    QuantityUnit = model.QuantityUnit,
                    QuantityRecommended = model.QuantityRecommended,
                    Status = model.Status,
                    QuantityCompleted = model.QuantityCompleted,
                    CompletedAtUtc = model.CompletedAtLocal?.ToUniversalTime(),
                    CompletedByUserId = model.CompletedByUserId,
                    SourcePrepTaskId = model.SourcePrepTaskId,
                    SourceDoughBatchId = model.SourceDoughBatchId,
                    Notes = model.Notes,
                    UpdatedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Prep task correction saved.");
            return RedirectToAction(nameof(Index), new { referenceDate = model.ReferenceDate.ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            ApplyPrepTaskOptions(model);
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("EditPrepTask", model);
        }
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)}")]
    [HttpGet("dough-batches/{id:guid}/edit")]
    public async Task<IActionResult> EditDoughBatch(
        Guid id,
        DateOnly? referenceDate,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;

        var batches = await _doughBatchReadRepository.SearchForCorrectionAsync(null, null, includeVoided: true, cancellationToken);
        var batch = batches.FirstOrDefault(item => item.Id == id);
        if (batch is null)
        {
            SetStatusMessage("danger", "The requested dough batch could not be found.");
            return RedirectToAction(nameof(Index), new { referenceDate });
        }

        return View("EditDoughBatch", new AdminDoughBatchCorrectionFormViewModel
        {
            DoughBatchId = batch.Id,
            ReferenceDate = referenceDate ?? batch.BatchDate,
            BatchDate = batch.BatchDate,
            TotalCases = batch.TotalCases,
            IsBalled = batch.IsBalled,
            BalledAtLocal = batch.BalledAtUtc?.ToLocalTime(),
            IsEventException = batch.IsEventException,
            IsVoided = batch.IsVoided,
            VoidReason = batch.VoidReason,
            Notes = batch.Notes
        });
    }

    [Authorize(Roles = $"{nameof(ApplicationRole.Admin)}")]
    [HttpPost("dough-batches/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDoughBatch(
        Guid id,
        AdminDoughBatchCorrectionFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        ViewData["UseDoughExperience"] = true;
        model.DoughBatchId = id;

        if (!ModelState.IsValid)
        {
            return View("EditDoughBatch", model);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        try
        {
            await _doughCorrectionAdminService.CorrectDoughBatchAsync(
                new AdminCorrectDoughBatchRequest
                {
                    DoughBatchId = id,
                    BatchDate = model.BatchDate,
                    TotalCases = model.TotalCases,
                    IsBalled = model.IsBalled,
                    BalledAtUtc = model.BalledAtLocal?.ToUniversalTime(),
                    IsEventException = model.IsEventException,
                    IsVoided = model.IsVoided,
                    VoidReason = model.VoidReason,
                    Notes = model.Notes,
                    UpdatedByUserId = currentUserId
                },
                cancellationToken);

            SetStatusMessage("success", "Dough batch correction saved.");
            return RedirectToAction(nameof(Index), new { referenceDate = model.ReferenceDate.ToString("yyyy-MM-dd") });
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("EditDoughBatch", model);
        }
    }

    private async Task<AdminDoughCorrectionsPageViewModel> BuildIndexViewModelAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken)
    {
        var weekStartDate = GetOperationalWeekStart(referenceDate);
        var traySchema = await InspectTrayCountSchemaAsync(cancellationToken);

        var weeklyCalendar = await _prepWeeklyDoughCalendarService.GetWeekAsync(referenceDate, 8, cancellationToken);
        var availability = await _doughAvailabilityProjectionService.GetWeeklyAvailabilityAsync(referenceDate, cancellationToken);
        var planning = await _doughProductionPlanningService.PlanAsync(
            new DoughProductionPlanningRequest
            {
                ProductionDate = referenceDate,
                DaysAhead = 6
            },
            cancellationToken);
        var carryover = await _weeklyDoughClosingReadService.GetCarryoverForWeekAsync(
            new GetWeeklyDoughCarryoverRequest
            {
                WeekStartDate = referenceDate
            },
            cancellationToken);
        var dailySummary = await _dailyDoughClosingReadService.GetWeekSummaryAsync(
            new GetDailyClosingWeekSummaryRequest
            {
                ReferenceDate = referenceDate,
                HistoricalWeeksToUse = 8
            },
            cancellationToken);
        var prepTasks = await _prepTaskReadService.SearchAsync(new SearchPrepTasksRequest(), cancellationToken);
        var batches = await _doughBatchReadRepository.SearchForCorrectionAsync(referenceDate.AddDays(-21), referenceDate, includeVoided: true, cancellationToken);
        var usageTraces = await _doughUsageTraceReadService.SearchAsync(
            new SearchDoughUsageTracesRequest
            {
                UsageDateFrom = weekStartDate,
                UsageDateTo = referenceDate
            },
            cancellationToken);
        var qualityRecords = await _doughQualityReadService.SearchAsync(
            new SearchDoughBatchQualityRecordsRequest
            {
                SourceDateFrom = referenceDate.AddDays(-21),
                SourceDateTo = referenceDate
            },
            cancellationToken);
        var weeklyClosings = await _weeklyDoughClosingReadService.GetWeeklyClosingsAsync(
            new GetWeeklyClosingsRequest
            {
                FromWeekStartDate = referenceDate.AddDays(-56),
                ToWeekStartDate = referenceDate
            },
            cancellationToken);

        return new AdminDoughCorrectionsPageViewModel
        {
            ReferenceDate = referenceDate,
            IsAdmin = User.IsInRole(nameof(ApplicationRole.Admin)),
            HasPendingFractionalTrayMigration = !string.Equals(traySchema.StorageType, "decimal", StringComparison.OrdinalIgnoreCase),
            TrayCountStorageType = traySchema.DisplayType,
            WeeklyCalendar = weeklyCalendar,
            Availability = availability,
            ProductionPlanning = planning,
            Carryover = carryover,
            DailySummary = dailySummary,
            PrepTasks = prepTasks
                .Where(task => task.TaskDate >= referenceDate.AddDays(-14) && task.TaskDate <= referenceDate.AddDays(1))
                .OrderByDescending(task => task.TaskDate)
                .ThenByDescending(task => task.CreatedAtUtc)
                .Take(18)
                .ToArray(),
            DoughBatches = batches.Take(18).ToArray(),
            UsageTraces = usageTraces.Take(18).ToArray(),
            QualityRecords = qualityRecords
                .OrderByDescending(record => record.SourceDate)
                .ThenByDescending(record => record.CreatedOrBalledAt)
                .Take(18)
                .ToArray(),
            WeeklyClosings = weeklyClosings.Take(8).ToArray()
        };
    }

    private AdminPrepTaskCorrectionFormViewModel BuildPrepTaskForm(
        Contracts.Responses.Prep.DoughTaskListItemResponse task,
        DateOnly referenceDate)
    {
        var model = new AdminPrepTaskCorrectionFormViewModel
        {
            PrepTaskId = task.PrepTaskId,
            ReferenceDate = referenceDate,
            TaskDate = task.TaskDate,
            TaskType = task.TaskType,
            QuantityUnit = task.QuantityUnit,
            QuantityRecommended = task.QuantityRecommended,
            Status = task.Status,
            QuantityCompleted = task.QuantityCompleted,
            CompletedAtLocal = task.CompletedAtUtc?.ToLocalTime(),
            CompletedByUserId = task.CompletedByUserId,
            SourcePrepTaskId = task.SourcePrepTaskId,
            SourceDoughBatchId = task.SourceDoughBatchId,
            Notes = task.Notes
        };

        ApplyPrepTaskOptions(model);
        return model;
    }

    private static void ApplyPrepTaskOptions(AdminPrepTaskCorrectionFormViewModel model)
    {
        model.TaskTypeOptions =
        [
            nameof(PrepTaskType.GenericDough),
            nameof(PrepTaskType.MakeDoughLoad),
            nameof(PrepTaskType.BallDough)
        ];

        model.QuantityUnitOptions =
        [
            nameof(DoughQuantityUnit.Balls),
            nameof(DoughQuantityUnit.FullLoads)
        ];

        model.StatusOptions =
        [
            nameof(PrepTaskStatus.Pending),
            nameof(PrepTaskStatus.InProgress),
            nameof(PrepTaskStatus.Completed),
            nameof(PrepTaskStatus.Cancelled)
        ];
    }

    private async Task<(string StorageType, string DisplayType)> InspectTrayCountSchemaAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return ("unknown", "unknown");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TOP 1 [DATA_TYPE], [NUMERIC_PRECISION], [NUMERIC_SCALE]
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE [TABLE_NAME] = 'DoughUsageTraces'
              AND [COLUMN_NAME] = 'TrayCount'
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return ("missing", "missing");
        }

        var type = reader.GetString(0);
        if (reader.IsDBNull(1) || reader.IsDBNull(2))
        {
            return (type, type);
        }

        return (type, $"{type}({reader.GetByte(1)},{reader.GetInt32(2)})");
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is ArgumentException
            or InvalidOperationException
            or KeyNotFoundException;
    }

    private static DateOnly GetOperationalWeekStart(DateOnly referenceDate)
    {
        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private void SetStatusMessage(string type, string message)
    {
        TempData[StatusTypeKey] = type;
        TempData[StatusMessageKey] = message;
    }
}
