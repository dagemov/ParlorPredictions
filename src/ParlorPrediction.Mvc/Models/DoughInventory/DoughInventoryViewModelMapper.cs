using ParlorPrediction.Contracts.Responses.Dough;

namespace ParlorPrediction.Mvc.Models.DoughInventory;

public static class DoughInventoryViewModelMapper
{
    public static DoughInventorySummaryViewModel MapSummary(DoughInventoryImpactResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new DoughInventorySummaryViewModel
        {
            ReferenceDate = response.ReferenceDate,
            WeekStartDate = response.WeekStartDate,
            WeekEndDate = response.WeekEndDate,
            WeeklyGoalBalls = response.WeeklyGoalBalls,
            ReadyNowBalls = response.ReadyNowBalls,
            StillMissingBalls = response.StillMissingBalls,
            UsedTodayBalls = response.UsedTodayBalls,
            UseFirstBalls = response.UseFirstBalls,
            AttentionBalls = response.AttentionBalls,
            MixedButNotBalledBalls = response.MixedButNotBalledBalls,
            FutureBalls = response.FutureBalls,
            LostOrDiscardedBalls = response.LostOrDiscardedBalls,
            RemainingTrackedBalls = response.RemainingTrackedBalls
        };
    }

    public static DoughInventoryPageViewModel MapPage(
        DoughInventoryImpactResponse response,
        int historicalWeeksToUse)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new DoughInventoryPageViewModel
        {
            TargetDate = response.ReferenceDate,
            HistoricalWeeksToUse = historicalWeeksToUse < 1 ? 8 : historicalWeeksToUse,
            Summary = MapSummary(response),
            Sources = response.RemainingSources
                .Select(source => new DoughInventorySourceCardViewModel
                {
                    SourceDoughBatchQualityRecordId = source.SourceDoughBatchQualityRecordId,
                    SourceDate = source.SourceDate,
                    CreatedOrBalledAt = source.CreatedOrBalledAt,
                    SourceType = source.SourceType,
                    MustUseByDate = source.MustUseByDate,
                    AgeDays = source.AgeDays,
                    OriginalBalls = source.OriginalBalls,
                    UsedBalls = source.UsedBalls,
                    RemainingBalls = source.RemainingBalls,
                    CountsAsAvailable = source.CountsAsAvailable,
                    IsReballCandidate = source.IsReballCandidate,
                    IsDiscardCandidate = source.IsDiscardCandidate,
                    RecommendedAction = source.RecommendedAction
                })
                .ToArray()
        };
    }
}
