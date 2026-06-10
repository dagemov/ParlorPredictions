using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Dough;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Contracts.Requests.DoughClosing;
using ParlorPrediction.Contracts.Responses.DoughClosing;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Application.Services.Dough;

public sealed class DailyDoughClosingManagementService : IDailyDoughClosingManagementService
{
    private readonly IDailyDoughClosingRepository _dailyDoughClosingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public DailyDoughClosingManagementService(
        IDailyDoughClosingRepository dailyDoughClosingRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _dailyDoughClosingRepository = dailyDoughClosingRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<DailyDoughClosingResponse> CreateDailyClosingAsync(
        CreateDailyDoughClosingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredAuthorizedUserAsync(request.ClosedByUserId, cancellationToken);
        var weekStartDate = NormalizeOperationalWeekStart(request.ClosingDate);
        var existingClosing = await _dailyDoughClosingRepository.GetByClosingDateAsync(request.ClosingDate, cancellationToken);

        if (existingClosing is not null)
        {
            throw new InvalidOperationException("A daily dough closing already exists for the requested date.");
        }

        var closing = DailyDoughClosing.Create(
            request.ClosingDate,
            weekStartDate,
            request.ForecastNeededBalls,
            request.ActualUsedBalls,
            user.Id,
            request.ClosedAtUtc ?? DateTime.UtcNow,
            request.Notes);

        await _dailyDoughClosingRepository.AddAsync(closing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(closing);
    }

    public async Task<DailyDoughClosingResponse> CorrectDailyClosingAsync(
        CorrectDailyDoughClosingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await GetRequiredAuthorizedUserAsync(request.CorrectedByUserId, cancellationToken);
        var closing = await GetRequiredClosingAsync(request.DailyDoughClosingId, cancellationToken);

        closing.Correct(
            request.ForecastNeededBalls,
            request.ActualUsedBalls,
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
            throw new InvalidOperationException("Only manager or admin users can close or correct daily dough.");
        }

        return user;
    }

    private async Task<DailyDoughClosing> GetRequiredClosingAsync(Guid dailyDoughClosingId, CancellationToken cancellationToken)
    {
        if (dailyDoughClosingId == Guid.Empty)
        {
            throw new ArgumentException("Daily dough closing id is required.", nameof(dailyDoughClosingId));
        }

        return await _dailyDoughClosingRepository.GetByIdAsync(dailyDoughClosingId, cancellationToken)
            ?? throw new KeyNotFoundException("The daily dough closing could not be found.");
    }

    private static DateOnly NormalizeOperationalWeekStart(DateOnly referenceDate)
    {
        if (referenceDate == default)
        {
            throw new ArgumentException("Closing date is required.", nameof(referenceDate));
        }

        var diff = ((int)referenceDate.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return referenceDate.AddDays(-diff);
    }

    private static DailyDoughClosingResponse Map(DailyDoughClosing closing)
    {
        return new DailyDoughClosingResponse
        {
            Id = closing.Id,
            ClosingDate = closing.ClosingDate,
            WeekStartDate = closing.WeekStartDate,
            ForecastNeededBalls = closing.ForecastNeededBalls,
            ActualUsedBalls = closing.ActualUsedBalls,
            DailyVariance = closing.DailyVariance,
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
