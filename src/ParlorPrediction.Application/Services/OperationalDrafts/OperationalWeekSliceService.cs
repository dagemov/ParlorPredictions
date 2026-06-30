using System.Text.Json;
using ParlorPrediction.Application.Interfaces.Ai;

namespace ParlorPrediction.Application.Services.OperationalDrafts;

public sealed class OperationalWeekSliceService : IOperationalWeekSliceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOperationalDraftService _operationalDraftService;

    public OperationalWeekSliceService(IOperationalDraftService operationalDraftService)
    {
        _operationalDraftService = operationalDraftService;
    }

    public async Task<OperationalWeekSliceResult> ExecuteAsync(
        OperationalWeekSliceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActorUserId);

        var correlationId = request.CorrelationId ?? Guid.NewGuid();
        var productionDrafts = new List<OperationalDraftEnvelope>(request.ProductionDrafts.Count);
        var dailyClosingDrafts = new List<OperationalDraftEnvelope>(request.DailyClosingDrafts.Count);
        var eventDrafts = new List<OperationalDraftEnvelope>(request.EventDrafts.Count);

        foreach (var productionDraft in request.ProductionDrafts)
        {
            productionDrafts.Add(await _operationalDraftService.CreateDoughTaskDraftAsync(
                productionDraft with
                {
                    CorrelationId = correlationId,
                    HistoricalWeeksToUse = productionDraft.HistoricalWeeksToUse < 1
                        ? request.HistoricalWeeksToUse
                        : productionDraft.HistoricalWeeksToUse,
                    ActorUserId = string.IsNullOrWhiteSpace(productionDraft.ActorUserId)
                        ? request.ActorUserId
                        : productionDraft.ActorUserId
                },
                cancellationToken));
        }

        foreach (var dailyClosingDraft in request.DailyClosingDrafts)
        {
            dailyClosingDrafts.Add(await _operationalDraftService.CreateDailyClosingDraftAsync(
                dailyClosingDraft with
                {
                    CorrelationId = correlationId,
                    HistoricalWeeksToUse = dailyClosingDraft.HistoricalWeeksToUse < 1
                        ? request.HistoricalWeeksToUse
                        : dailyClosingDraft.HistoricalWeeksToUse,
                    ActorUserId = string.IsNullOrWhiteSpace(dailyClosingDraft.ActorUserId)
                        ? request.ActorUserId
                        : dailyClosingDraft.ActorUserId
                },
                cancellationToken));
        }

        foreach (var eventDraft in request.EventDrafts)
        {
            eventDrafts.Add(await _operationalDraftService.CreateRestaurantEventDraftAsync(
                eventDraft with
                {
                    CorrelationId = correlationId,
                    ActorUserId = string.IsNullOrWhiteSpace(eventDraft.ActorUserId)
                        ? request.ActorUserId
                        : eventDraft.ActorUserId
                },
                cancellationToken));
        }

        var weeklyClosingDraft = await _operationalDraftService.CreateWeeklyClosingPreviewDraftAsync(
            new OperationalWeeklyClosingPreviewRequest
            {
                CorrelationId = correlationId,
                ReferenceDate = request.ReferenceDate,
                WeekStartDate = request.WeekStartDate,
                HistoricalWeeksToUse = request.HistoricalWeeksToUse,
                ActorUserId = request.ActorUserId,
                Notes = request.WeeklyClosingNotes
            },
            cancellationToken);

        return new OperationalWeekSliceResult
        {
            CorrelationId = correlationId,
            ProductionDrafts = productionDrafts,
            DailyClosingDrafts = dailyClosingDrafts,
            EventDrafts = eventDrafts,
            WeeklyClosingDraft = weeklyClosingDraft,
            ValidationWarnings =
                productionDrafts
                    .Concat(dailyClosingDrafts)
                    .Concat(eventDrafts)
                    .Append(weeklyClosingDraft)
                    .SelectMany(GetWarnings)
                    .ToArray()
        };
    }

    private static IReadOnlyList<OperationalValidationWarning> GetWarnings(OperationalDraftEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.Draft.ValidationWarningsJson))
        {
            return Array.Empty<OperationalValidationWarning>();
        }

        return JsonSerializer.Deserialize<IReadOnlyList<OperationalValidationWarning>>(
                   envelope.Draft.ValidationWarningsJson,
                   JsonOptions)
               ?? Array.Empty<OperationalValidationWarning>();
    }
}
