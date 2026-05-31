using ParlorPrediction.Application.Interfaces.Auth;
using ParlorPrediction.Application.Interfaces.Persistence;
using ParlorPrediction.Application.Interfaces.Prep;
using ParlorPrediction.Contracts.Requests.Prep;
using ParlorPrediction.Contracts.Responses.Prep;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Application.Services.Prep;

public sealed class ManagerPrepRecommendationService : IManagerPrepRecommendationService
{
    private readonly IManagerPrepRecommendationRepository _managerPrepRecommendationRepository;
    private readonly IPrepItemReadRepository _prepItemReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public ManagerPrepRecommendationService(
        IManagerPrepRecommendationRepository managerPrepRecommendationRepository,
        IPrepItemReadRepository prepItemReadRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _managerPrepRecommendationRepository = managerPrepRecommendationRepository;
        _prepItemReadRepository = prepItemReadRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<Guid> CreateAsync(
        SaveManagerPrepRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RecommendationDate == default)
        {
            throw new ArgumentException("Recommendation date is required.", nameof(request));
        }

        if (request.PrepItemId == Guid.Empty)
        {
            throw new ArgumentException("Prep item is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.CreatedByUserId))
        {
            throw new ArgumentException("Created by user id is required.", nameof(request));
        }

        var prepItem = await _prepItemReadRepository.GetByIdAsync(request.PrepItemId, cancellationToken)
            ?? throw new KeyNotFoundException("The prep item could not be found.");

        var user = await _userRepository.FindByIdAsync(request.CreatedByUserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("The manager recommendation user could not be found or is inactive.");
        }

        var recommendation = ManagerPrepRecommendation.Create(
            request.RecommendationDate,
            prepItem.Id,
            request.RecommendationText,
            request.RecommendedBalls,
            request.Reason,
            user.Id);

        await _managerPrepRecommendationRepository.AddAsync(recommendation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return recommendation.Id;
    }

    public async Task<IReadOnlyList<ManagerPrepRecommendationListItemResponse>> SearchAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? prepItemId,
        int take = 12,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await _managerPrepRecommendationRepository.SearchAsync(
            fromDate,
            toDate,
            prepItemId,
            cancellationToken);

        return recommendations
            .Take(take < 1 ? 12 : take)
            .Select(Map)
            .ToArray();
    }

    private static ManagerPrepRecommendationListItemResponse Map(ManagerPrepRecommendation recommendation)
    {
        return new ManagerPrepRecommendationListItemResponse
        {
            Id = recommendation.Id,
            RecommendationDate = recommendation.RecommendationDate,
            PrepItemId = recommendation.PrepItemId,
            PrepItemName = recommendation.PrepItem.Name,
            RecommendationText = recommendation.RecommendationText,
            RecommendedBalls = recommendation.RecommendedBalls,
            RecommendedCases = recommendation.RecommendedCases,
            RecommendedLoads = recommendation.RecommendedLoads,
            Reason = recommendation.Reason,
            CreatedByUserId = recommendation.CreatedByUserId,
            CreatedByUserName = recommendation.CreatedByUser.FullName,
            CreatedAtUtc = recommendation.CreatedAtUtc
        };
    }
}
