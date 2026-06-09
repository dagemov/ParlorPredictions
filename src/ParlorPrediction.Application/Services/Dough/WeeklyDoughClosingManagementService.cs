using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class WeeklyDoughClosingManagementService : IWeeklyDoughClosingManagementService
{
    private readonly IWeeklyDoughClosingRepository _weeklyDoughClosingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public WeeklyDoughClosingManagementService(
        IWeeklyDoughClosingRepository weeklyDoughClosingRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _weeklyDoughClosingRepository = weeklyDoughClosingRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<WeeklyDoughClosingResponse> CreateWeeklyClosingAsync(
        CreateWeeklyDoughClosingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredAuthorizedUserAsync(request.ClosedByUserId, cancellationToken);
        var weekStartDate = NormalizeOperationalWeekStart(request.WeekStartDate);
        var existingClosing = await _weeklyDoughClosingRepository.GetByWeekStartDateAsync(weekStartDate, cancellationToken);

        if (existingClosing is not null)
        {
            throw new InvalidOperationException("A weekly dough closing already exists for the requested week.");
        }

        var closing = WeeklyDoughClosing.Create(
            weekStartDate,
            request.NeededBalls,
            request.ProducedBalls,
            request.UsedBalls,
            request.LostBalls,
            request.LeftoverReadyBalls,
            request.LeftoverAttentionBalls,
            request.LeftoverMixedLoads,
            user.Id,
            request.ClosedAtUtc ?? DateTime.UtcNow,
            request.Notes);

        await _weeklyDoughClosingRepository.AddAsync(closing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(closing);
    }

    public async Task<WeeklyDoughClosingResponse> CorrectWeeklyClosingAsync(
        CorrectWeeklyDoughClosingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredAuthorizedUserAsync(request.CorrectedByUserId, cancellationToken);
        var closing = await GetRequiredClosingAsync(request.WeeklyDoughClosingId, cancellationToken);

        closing.Correct(
            request.NeededBalls,
            request.ProducedBalls,
            request.UsedBalls,
            request.LostBalls,
            request.LeftoverReadyBalls,
            request.LeftoverAttentionBalls,
            request.LeftoverMixedLoads,
            user.Id,
            request.CorrectedAtUtc ?? DateTime.UtcNow,
            request.Notes,
            request.CorrectionNote);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(closing);
    }

    private async Task<User> GetRequiredAuthorizedUserAsync(string userId, CancellationToken cancellationToken)
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

        if (user.Role is not ApplicationRole.Admin and not ApplicationRole.Manager)
        {
            throw new InvalidOperationException("Only manager or admin users can close or correct weekly dough.");
        }

        return user;
    }

    private async Task<WeeklyDoughClosing> GetRequiredClosingAsync(Guid weeklyDoughClosingId, CancellationToken cancellationToken)
    {
        if (weeklyDoughClosingId == Guid.Empty)
        {
            throw new ArgumentException("Weekly dough closing id is required.", nameof(weeklyDoughClosingId));
        }

        return await _weeklyDoughClosingRepository.GetByIdAsync(weeklyDoughClosingId, cancellationToken)
            ?? throw new KeyNotFoundException("The weekly dough closing could not be found.");
    }

    private static DateOnly NormalizeOperationalWeekStart(DateOnly referenceDate)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Week start date is required.", nameof(referenceDate));
        }

        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private static WeeklyDoughClosingResponse Map(WeeklyDoughClosing closing)
    {
        return new WeeklyDoughClosingResponse
        {
            Id = closing.Id,
            WeekStartDate = closing.WeekStartDate,
            WeekEndDate = closing.WeekEndDate,
            NeededBalls = closing.NeededBalls,
            ProducedBalls = closing.ProducedBalls,
            UsedBalls = closing.UsedBalls,
            LostBalls = closing.LostBalls,
            LeftoverReadyBalls = closing.LeftoverReadyBalls,
            LeftoverAttentionBalls = closing.LeftoverAttentionBalls,
            LeftoverMixedLoads = closing.LeftoverMixedLoads,
            CarryoverAvailableBalls = closing.CarryoverAvailableBalls,
            Notes = closing.Notes,
            ClosedByUserId = closing.ClosedByUserId,
            ClosedAtUtc = closing.ClosedAtUtc,
            WasCorrected = closing.WasCorrected,
            CorrectedByUserId = closing.CorrectedByUserId,
            CorrectedAtUtc = closing.CorrectedAtUtc,
            CorrectionNote = closing.CorrectionNote
        };
    }
}
