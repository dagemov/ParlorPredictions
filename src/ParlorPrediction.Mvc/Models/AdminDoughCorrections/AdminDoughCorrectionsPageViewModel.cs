using ParlorPrediction.Contracts.Responses.Dough;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughUsage;
using ParlorPrediction.Contracts.Responses.Prep;

namespace ParlorPrediction.Mvc.Models.AdminDoughCorrections;

public sealed class AdminDoughCorrectionsPageViewModel
{
    public DateOnly ReferenceDate { get; set; }

    public bool IsAdmin { get; set; }

    public bool HasPendingFractionalTrayMigration { get; set; }

    public string TrayCountStorageType { get; set; } = "unknown";

    public string ManualCorrectionWarning { get; set; } = "Manual correction affects planning calculations.";

    public WeeklyDoughCalendarResponse? WeeklyCalendar { get; set; }

    public DoughAvailabilityProjectionResponse? Availability { get; set; }

    public DoughProductionPlanningResponse? ProductionPlanning { get; set; }

    public WeeklyDoughCarryoverResponse? Carryover { get; set; }

    public DailyClosingWeekSummaryResponse? DailySummary { get; set; }

    public IReadOnlyList<DoughTaskListItemResponse> PrepTasks { get; set; } = Array.Empty<DoughTaskListItemResponse>();

    public IReadOnlyList<Domain.Entities.DoughBatch> DoughBatches { get; set; } = Array.Empty<Domain.Entities.DoughBatch>();

    public IReadOnlyList<DoughUsageTraceResponse> UsageTraces { get; set; } = Array.Empty<DoughUsageTraceResponse>();

    public IReadOnlyList<DoughBatchQualityRecordResponse> QualityRecords { get; set; } = Array.Empty<DoughBatchQualityRecordResponse>();

    public IReadOnlyList<WeeklyDoughClosingResponse> WeeklyClosings { get; set; } = Array.Empty<WeeklyDoughClosingResponse>();
}
