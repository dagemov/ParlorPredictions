using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Requests.DoughQuality;
using ParlorPrediction.Contracts.Responses.DoughQuality;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DoughQualityManagementService : IDoughQualityManagementService
{
    private readonly IDoughBatchQualityRepository _doughBatchQualityRepository;
    private readonly IDoughLossRecordRepository _doughLossRecordRepository;
    private readonly IDoughReballRecordRepository _doughReballRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public DoughQualityManagementService(
        IDoughBatchQualityRepository doughBatchQualityRepository,
        IDoughLossRecordRepository doughLossRecordRepository,
        IDoughReballRecordRepository doughReballRecordRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _doughBatchQualityRepository = doughBatchQualityRepository;
        _doughLossRecordRepository = doughLossRecordRepository;
        _doughReballRecordRepository = doughReballRecordRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<Guid> CreateAsync(
        SaveDoughBatchQualityRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.CreatedByUserId, cancellationToken);
        var initialStatus = ParseDoughQualityStatus(request.InitialStatus, nameof(request.InitialStatus));
        var discardReason = ParseOptionalLossReason(request.DiscardReason);

        var record = DoughBatchQualityRecord.Create(
            request.SourceDate,
            request.CreatedOrBalledAt,
            request.QuantityBalls,
            user.Id,
            request.OriginalDoughTaskId,
            initialStatus,
            request.StatusReason,
            request.MustUseByDate,
            discardReason,
            request.ManagerNote);

        await _doughBatchQualityRepository.AddAsync(record, cancellationToken);

        if (initialStatus == DoughQualityStatus.Discarded)
        {
            await _doughLossRecordRepository.AddAsync(
                DoughLossRecord.Create(
                    record.Id,
                    record.QuantityBalls,
                    discardReason ?? DoughLossReason.ManagerDecision,
                    record.SourceDate,
                    user.Id,
                    request.ManagerNote),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return record.Id;
    }

    public async Task<DoughBatchQualityRecordResponse> MarkAsAttentionAsync(
        MarkDoughAsAttentionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.UpdatedByUserId, cancellationToken);
        var record = await GetRequiredRecordAsync(request.DoughBatchQualityRecordId, cancellationToken);

        record.MarkAttention(
            request.AttentionMarkedAtUtc ?? DateTime.UtcNow,
            request.StatusReason,
            user.Id,
            request.ManagerNote);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(record);
    }

    public async Task<DoughBatchQualityRecordResponse> CorrectStatusAsync(
        CorrectDoughQualityStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.UpdatedByUserId, cancellationToken);
        EnsureUserHasRole(user, ApplicationRole.Admin);

        var record = await GetRequiredRecordAsync(request.DoughBatchQualityRecordId, cancellationToken);
        var originalStatus = record.CurrentStatus;
        var newStatus = ParseDoughQualityStatus(request.NewStatus, nameof(request.NewStatus));
        var discardReason = ParseOptionalLossReason(request.DiscardReason);

        record.CorrectStatus(
            newStatus,
            user.Id,
            request.StatusReason,
            request.ManagerNote,
            request.EffectiveAtUtc,
            request.MustUseByDate,
            discardReason);

        if (newStatus == DoughQualityStatus.Discarded && originalStatus != DoughQualityStatus.Discarded)
        {
            await _doughLossRecordRepository.AddAsync(
                DoughLossRecord.Create(
                    record.Id,
                    record.QuantityBalls,
                    discardReason ?? DoughLossReason.ManagerDecision,
                    DateOnly.FromDateTime(request.EffectiveAtUtc ?? DateTime.UtcNow),
                    user.Id,
                    request.ManagerNote),
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(record);
    }

    public async Task<DoughBatchQualityRecordResponse> DiscardAsync(
        DiscardDoughRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.UpdatedByUserId, cancellationToken);
        EnsureUserHasRole(user, ApplicationRole.Manager, ApplicationRole.Admin);

        var record = await GetRequiredRecordAsync(request.DoughBatchQualityRecordId, cancellationToken);
        var discardReason = ParseLossReason(request.DiscardReason, nameof(request.DiscardReason));
        var discardedAt = request.DiscardedAtUtc ?? DateTime.UtcNow;

        record.Discard(discardReason, discardedAt, user.Id, request.ManagerNote);

        await _doughLossRecordRepository.AddAsync(
            DoughLossRecord.Create(
                record.Id,
                record.QuantityBalls,
                discardReason,
                DateOnly.FromDateTime(discardedAt),
                user.Id,
                request.ManagerNote),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(record);
    }

    public async Task<DoughBatchQualityRecordResponse> ReballAsync(
        ReballDoughRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredActiveUserAsync(request.UpdatedByUserId, cancellationToken);
        var result = ParseReballResult(request.Result, nameof(request.Result));

        if (result == ReballResult.PartialRecovered)
        {
            EnsureUserHasRole(user, ApplicationRole.Admin, ApplicationRole.Manager, ApplicationRole.PizzaMaker);
        }
        else
        {
            EnsureUserHasRole(user, ApplicationRole.Manager, ApplicationRole.Admin);
        }

        var record = await GetRequiredRecordAsync(request.DoughBatchQualityRecordId, cancellationToken);
        var quantityBeforeReball = record.QuantityBalls;
        var reballDate = request.ReballDateUtc == default
            ? DateTime.UtcNow
            : request.ReballDateUtc;

        switch (result)
        {
            case ReballResult.PartialRecovered:
            {
                record.ApplyPartialReball(request.QuantityRecoveredBalls, reballDate, user.Id, request.ManagerNote);

                await _doughReballRecordRepository.AddAsync(
                    DoughReballRecord.Create(
                        record.Id,
                        quantityBeforeReball,
                        request.QuantityRecoveredBalls,
                        DateOnly.FromDateTime(reballDate),
                        ReballResult.PartialRecovered,
                        user.Id,
                        record.MustUseByDate,
                        request.ManagerNote),
                    cancellationToken);

                var quantityLostBalls = quantityBeforeReball - request.QuantityRecoveredBalls;
                if (quantityLostBalls > 0)
                {
                    await _doughLossRecordRepository.AddAsync(
                        DoughLossRecord.Create(
                            record.Id,
                            quantityLostBalls,
                            ParseOptionalLossReason(request.DiscardReason) ?? DoughLossReason.ManagerDecision,
                            DateOnly.FromDateTime(reballDate),
                            user.Id,
                            request.ManagerNote),
                        cancellationToken);
                }

                break;
            }

            case ReballResult.Discarded:
            {
                var discardReason = ParseLossReason(request.DiscardReason ?? string.Empty, nameof(request.DiscardReason));
                record.Discard(discardReason, reballDate, user.Id, request.ManagerNote);

                await _doughReballRecordRepository.AddAsync(
                    DoughReballRecord.Create(
                        record.Id,
                        quantityBeforeReball,
                        0,
                        DateOnly.FromDateTime(reballDate),
                        ReballResult.Discarded,
                        user.Id,
                        null,
                        request.ManagerNote),
                    cancellationToken);

                await _doughLossRecordRepository.AddAsync(
                    DoughLossRecord.Create(
                        record.Id,
                        quantityBeforeReball,
                        discardReason,
                        DateOnly.FromDateTime(reballDate),
                        user.Id,
                        request.ManagerNote),
                    cancellationToken);

                break;
            }

            default:
                throw new InvalidOperationException("ManagerCancelled reball events are not persisted as a state change in this backend phase.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(record);
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

    private async Task<DoughBatchQualityRecord> GetRequiredRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Dough quality record id is required.", nameof(id));
        }

        return await _doughBatchQualityRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The dough quality record could not be found.");
    }

    private static void EnsureUserHasRole(User user, params ApplicationRole[] allowedRoles)
    {
        if (!allowedRoles.Contains(user.Role))
        {
            throw new InvalidOperationException("The acting user is not allowed to perform this dough quality action.");
        }
    }

    private static DoughQualityStatus ParseDoughQualityStatus(string value, string parameterName)
    {
        if (!DoughQualityStatusExtensions.TryParse(value, out var status))
        {
            throw new ArgumentException("The dough quality status is not valid.", parameterName);
        }

        return status;
    }

    private static DoughLossReason ParseLossReason(string value, string parameterName)
    {
        if (!DoughLossReasonExtensions.TryParse(value, out var reason))
        {
            throw new ArgumentException("The dough loss reason is not valid.", parameterName);
        }

        return reason;
    }

    private static DoughLossReason? ParseOptionalLossReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DoughLossReasonExtensions.TryParse(value, out var reason))
        {
            throw new ArgumentException("The dough loss reason is not valid.", nameof(value));
        }

        return reason;
    }

    private static ReballResult ParseReballResult(string value, string parameterName)
    {
        if (!ReballResultExtensions.TryParse(value, out var result))
        {
            throw new ArgumentException("The reball result is not valid.", parameterName);
        }

        return result;
    }

    private static DoughBatchQualityRecordResponse Map(DoughBatchQualityRecord record)
    {
        return new DoughBatchQualityRecordResponse
        {
            Id = record.Id,
            SourceDate = record.SourceDate,
            OriginalDoughTaskId = record.OriginalDoughTaskId,
            CreatedOrBalledAt = record.CreatedOrBalledAt,
            QuantityBalls = record.QuantityBalls,
            CurrentStatus = record.CurrentStatus.ToString(),
            StatusReason = record.StatusReason,
            AttentionMarkedAt = record.AttentionMarkedAt,
            ReballedAt = record.ReballedAt,
            MustUseByDate = record.MustUseByDate,
            DiscardedAt = record.DiscardedAt,
            DiscardReason = record.DiscardReason?.ToString(),
            ManagerNote = record.ManagerNote,
            CreatedByUserId = record.CreatedByUserId,
            UpdatedByUserId = record.UpdatedByUserId,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
            CountsAsAvailable = record.CountsAsAvailable
        };
    }
}
