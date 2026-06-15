using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Responses.DoughUsage;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughUsageTraceReadService : IDoughUsageTraceReadService
{
    private readonly IDoughSourceProjectionService _doughSourceProjectionService;
    private readonly IDoughUsageTraceRepository _doughUsageTraceRepository;

    public DoughUsageTraceReadService(
        IDoughSourceProjectionService doughSourceProjectionService,
        IDoughUsageTraceRepository doughUsageTraceRepository)
    {
        _doughSourceProjectionService = doughSourceProjectionService;
        _doughUsageTraceRepository = doughUsageTraceRepository;
    }

    public async Task<DoughUsageTraceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Dough usage trace id is required.", nameof(id));
        }

        var trace = await _doughUsageTraceRepository.GetByIdAsync(id, cancellationToken);
        return trace is null ? null : Map(trace);
    }

    public async Task<IReadOnlyList<DoughUsageTraceResponse>> SearchAsync(
        SearchDoughUsageTracesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var traces = await _doughUsageTraceRepository.SearchAsync(
            request.UsageDateFrom,
            request.UsageDateTo,
            request.SourceDoughBatchQualityRecordId,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Destination))
        {
            var destination = ParseDestination(request.Destination, nameof(request.Destination));
            traces = traces
                .Where(trace => trace.Destination == destination)
                .ToArray();
        }

        return traces
            .Select(Map)
            .ToArray();
    }

    public async Task<IReadOnlyList<DoughUsageSourceOptionResponse>> GetAvailableSourcesForDateAsync(
        GetAvailableDoughSourcesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UsageDate == default)
        {
            throw new ArgumentException("Usage date is required.", nameof(request));
        }

        var destination = ParseDestination(request.Destination, nameof(request.Destination));
        var sources = await _doughSourceProjectionService.GetRemainingBySourceAsync(request.UsageDate, cancellationToken);

        var options = sources
            .Where(source => source.CountsAsAvailable && source.RemainingBalls > 0)
            .Select(source =>
            {
                var warningMessage = BuildWarningMessage(source, request.UsageDate, destination);
                return new DoughUsageSourceOptionResponse
                {
                    SourceDoughBatchQualityRecordId = source.SourceDoughBatchQualityRecordId,
                    UsageDate = request.UsageDate,
                    SourceDate = source.SourceDate,
                    SourceType = source.SourceType,
                    AgeDays = source.AgeDays,
                    OriginalBalls = source.OriginalBalls,
                    UsedBalls = source.UsedBalls,
                    RemainingBalls = source.RemainingBalls,
                    RecommendedAction = source.RecommendedAction,
                    IsPreferredSource = IsPreferredSource(source, request.UsageDate, destination),
                    HasWarning = !string.IsNullOrWhiteSpace(warningMessage),
                    WarningMessage = warningMessage
                };
            });

        return destination switch
        {
            DoughUsageDestination.Restaurant => options
                .OrderBy(option => GetRestaurantPriority(option))
                .ThenBy(option => option.SourceDate)
                .ThenByDescending(option => option.RemainingBalls)
                .ToArray(),
            _ => options
                .OrderBy(option => option.HasWarning)
                .ThenBy(option => Math.Abs(option.AgeDays - 1))
                .ThenBy(option => option.SourceDate)
                .ThenByDescending(option => option.RemainingBalls)
                .ToArray()
        };
    }

    public Task<IReadOnlyList<DoughSourceRemainingResponse>> GetRemainingBySourceAsync(
        GetDoughRemainingBySourceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _doughSourceProjectionService.GetRemainingBySourceAsync(request.ReferenceDate, cancellationToken);
    }

    public async Task<DoughReballPlanningResponse> GetReballPlanningForDateAsync(
        GetDoughReballPlanningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReferenceDate == default)
        {
            throw new ArgumentException("Reference date is required.", nameof(request));
        }

        var sources = await _doughSourceProjectionService.GetRemainingBySourceAsync(request.ReferenceDate, cancellationToken);
        var activeSources = sources
            .Where(source => source.CountsAsAvailable && source.RemainingBalls > 0)
            .ToArray();

        return new DoughReballPlanningResponse
        {
            ReferenceDate = request.ReferenceDate,
            MustUseFirstSources = activeSources
                .Where(source => string.Equals(source.RecommendedAction, DoughActionRecommendation.UseFirst.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(source => source.SourceDate)
                .ToArray(),
            ReviewSources = activeSources
                .Where(source => string.Equals(source.RecommendedAction, DoughActionRecommendation.Review.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(source => source.SourceDate)
                .ToArray(),
            ReballCandidates = activeSources
                .Where(source => string.Equals(source.RecommendedAction, DoughActionRecommendation.Reball.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(source => source.SourceDate)
                .ToArray(),
            DiscardCandidates = activeSources
                .Where(source => string.Equals(source.RecommendedAction, DoughActionRecommendation.Discard.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(source => source.SourceDate)
                .ToArray()
        };
    }

    private static int GetRestaurantPriority(DoughUsageSourceOptionResponse option)
    {
        if (string.Equals(option.SourceType, DoughQualityStatus.MustUseNextDay.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(option.SourceType, DoughQualityStatus.Reballed.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(option.RecommendedAction, DoughActionRecommendation.Review.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
    }

    private static bool IsPreferredSource(
        DoughSourceRemainingResponse source,
        DateOnly usageDate,
        DoughUsageDestination destination)
    {
        return destination switch
        {
            DoughUsageDestination.Restaurant => string.Equals(source.SourceType, DoughQualityStatus.MustUseNextDay.ToString(), StringComparison.OrdinalIgnoreCase),
            _ => !IsSummerRiskSource(source, usageDate) && source.AgeDays <= 1
        };
    }

    private static string? BuildWarningMessage(
        DoughSourceRemainingResponse source,
        DateOnly usageDate,
        DoughUsageDestination destination)
    {
        if (destination == DoughUsageDestination.Restaurant)
        {
            return null;
        }

        if (!IsSummerRiskSource(source, usageDate))
        {
            return null;
        }

        return "Summer event service prefers fresh or previous-day dough. This source is older or reballed and may ferment too quickly in heat and humidity.";
    }

    private static bool IsSummerRiskSource(DoughSourceRemainingResponse source, DateOnly usageDate)
    {
        if (!DoughRules.IsSummerEventMonth(usageDate.Month))
        {
            return false;
        }

        return !string.Equals(source.SourceType, DoughQualityStatus.Good.ToString(), StringComparison.OrdinalIgnoreCase) ||
            source.AgeDays > 1;
    }

    private static DoughUsageDestination ParseDestination(string value, string parameterName)
    {
        if (!DoughUsageDestinationExtensions.TryParse(value, out var destination))
        {
            throw new ArgumentException("The dough usage destination is not valid.", parameterName);
        }

        return destination;
    }

    private static DoughUsageTraceResponse Map(DoughUsageTrace trace)
    {
        return new DoughUsageTraceResponse
        {
            Id = trace.Id,
            UsageDate = trace.UsageDate,
            SourceDoughBatchQualityRecordId = trace.SourceDoughBatchQualityRecordId,
            SourceDate = trace.SourceDate,
            SourceType = trace.SourceType.ToString(),
            Destination = trace.Destination.ToString(),
            TrayCount = trace.TrayCount,
            BallsPerTray = trace.BallsPerTray,
            BallsUsed = trace.BallsUsed,
            Notes = trace.Notes,
            CreatedByUserId = trace.CreatedByUserId,
            UpdatedByUserId = trace.UpdatedByUserId,
            CreatedAtUtc = trace.CreatedAtUtc,
            UpdatedAtUtc = trace.UpdatedAtUtc
        };
    }
}
