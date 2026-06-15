using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Requests.DoughUsage;
using ParlorPrediction.Contracts.Responses.DoughUsage;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;
using ParlorPrediction.Domain.Rules;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughUsageTraceManagementService : IDoughUsageTraceManagementService
{
    private readonly IDoughBatchQualityRepository _doughBatchQualityRepository;
    private readonly IDoughUsageTraceRepository _doughUsageTraceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public DoughUsageTraceManagementService(
        IDoughBatchQualityRepository doughBatchQualityRepository,
        IDoughUsageTraceRepository doughUsageTraceRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _doughBatchQualityRepository = doughBatchQualityRepository;
        _doughUsageTraceRepository = doughUsageTraceRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<DoughUsageTraceResponse> CreateAsync(
        CreateDoughUsageTraceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.CreatedByUserId, cancellationToken);
        EnsureUserHasRole(user, ApplicationRole.Admin, ApplicationRole.Manager, ApplicationRole.PizzaMaker);

        var sourceRecord = await GetRequiredSourceRecordAsync(request.SourceDoughBatchQualityRecordId, cancellationToken);
        ValidateUsageSource(sourceRecord, request.UsageDate);

        var ballsUsed = CalculateBallsUsed(request.TrayCount);
        await EnsureSourceHasCapacityAsync(sourceRecord, ballsUsed, null, cancellationToken);

        var trace = DoughUsageTrace.Create(
            request.UsageDate,
            sourceRecord.Id,
            sourceRecord.SourceDate,
            sourceRecord.CurrentStatus,
            ParseDestination(request.Destination, nameof(request.Destination)),
            request.TrayCount,
            user.Id,
            request.Notes);

        await _doughUsageTraceRepository.AddAsync(trace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(trace);
    }

    public async Task<DoughUsageTraceResponse> CorrectAsync(
        CorrectDoughUsageTraceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.UpdatedByUserId, cancellationToken);
        EnsureUserHasRole(user, ApplicationRole.Admin, ApplicationRole.Manager);

        var trace = await GetRequiredTraceAsync(request.DoughUsageTraceId, cancellationToken);
        var sourceRecord = await GetRequiredSourceRecordAsync(request.SourceDoughBatchQualityRecordId, cancellationToken);
        ValidateUsageSource(sourceRecord, request.UsageDate);

        var ballsUsed = CalculateBallsUsed(request.TrayCount);
        await EnsureSourceHasCapacityAsync(sourceRecord, ballsUsed, trace.Id, cancellationToken);

        trace.Correct(
            request.UsageDate,
            sourceRecord.Id,
            sourceRecord.SourceDate,
            sourceRecord.CurrentStatus,
            ParseDestination(request.Destination, nameof(request.Destination)),
            request.TrayCount,
            user.Id,
            request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(trace);
    }

    public async Task DeleteAsync(
        DeleteDoughUsageTraceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.DeletedByUserId, cancellationToken);
        EnsureUserHasRole(user, ApplicationRole.Admin, ApplicationRole.Manager);

        var trace = await GetRequiredTraceAsync(request.DoughUsageTraceId, cancellationToken);
        _doughUsageTraceRepository.Remove(trace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetRequiredActiveUserAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("The acting user could not be found or is inactive.");
        }

        return user;
    }

    private async Task<DoughBatchQualityRecord> GetRequiredSourceRecordAsync(Guid sourceRecordId, CancellationToken cancellationToken)
    {
        if (sourceRecordId == Guid.Empty)
        {
            throw new ArgumentException("Source dough quality record id is required.", nameof(sourceRecordId));
        }

        return await _doughBatchQualityRepository.GetByIdAsync(sourceRecordId, cancellationToken)
            ?? throw new KeyNotFoundException("The selected dough source could not be found.");
    }

    private async Task<DoughUsageTrace> GetRequiredTraceAsync(Guid traceId, CancellationToken cancellationToken)
    {
        if (traceId == Guid.Empty)
        {
            throw new ArgumentException("Dough usage trace id is required.", nameof(traceId));
        }

        return await _doughUsageTraceRepository.GetByIdAsync(traceId, cancellationToken)
            ?? throw new KeyNotFoundException("The dough usage trace could not be found.");
    }

    private async Task EnsureSourceHasCapacityAsync(
        DoughBatchQualityRecord sourceRecord,
        int requestedBallsUsed,
        Guid? excludedTraceId,
        CancellationToken cancellationToken)
    {
        var traces = await _doughUsageTraceRepository.SearchAsync(
            null,
            null,
            sourceRecord.Id,
            cancellationToken);

        var existingUsedBalls = traces
            .Where(trace => excludedTraceId is null || trace.Id != excludedTraceId.Value)
            .Sum(trace => trace.BallsUsed);

        var remainingBalls = Math.Max(sourceRecord.QuantityBalls - existingUsedBalls, 0);
        if (requestedBallsUsed > remainingBalls)
        {
            throw new InvalidOperationException($"Cannot use {requestedBallsUsed} balls from this source because only {remainingBalls} remain.");
        }
    }

    private static void EnsureUserHasRole(User user, params ApplicationRole[] allowedRoles)
    {
        if (!allowedRoles.Contains(user.Role))
        {
            throw new InvalidOperationException("The acting user is not allowed to perform this dough usage action.");
        }
    }

    private static void ValidateUsageSource(DoughBatchQualityRecord sourceRecord, DateOnly usageDate)
    {
        if (usageDate == default)
        {
            throw new ArgumentException("Usage date is required.", nameof(usageDate));
        }

        if (!sourceRecord.CountsAsAvailable || sourceRecord.CurrentStatus == DoughQualityStatus.Discarded)
        {
            throw new InvalidOperationException("Discarded dough cannot be selected as a usage source.");
        }

        var availableFromDate = DateOnly.FromDateTime(sourceRecord.CreatedOrBalledAt.ToLocalTime());
        if (usageDate < availableFromDate)
        {
            throw new InvalidOperationException("A dough source cannot be used before it was created or balled.");
        }
    }

    private static DoughUsageDestination ParseDestination(string value, string parameterName)
    {
        if (!DoughUsageDestinationExtensions.TryParse(value, out var destination))
        {
            throw new ArgumentException("The dough usage destination is not valid.", parameterName);
        }

        return destination;
    }

    private static int CalculateBallsUsed(int trayCount)
    {
        return DoughRules.ConvertToBalls(trayCount, DoughQuantityUnit.Cases);
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
