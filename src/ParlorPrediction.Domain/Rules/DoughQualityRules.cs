using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Domain.Rules;

public static class DoughQualityRules
{
    public const int AttentionCandidateMinimumDays = 3;
    public const int AttentionCandidatePreferredMaximumDays = 4;
    public const int ReballMustUseOffsetDays = 1;

    public static bool CountsAsAvailable(DoughQualityStatus status)
    {
        return status is not DoughQualityStatus.Discarded;
    }

    public static int CalculateOperationalAgeDays(DateTime createdOrBalledAt, DateOnly referenceDate)
    {
        return referenceDate.DayNumber - DateOnly.FromDateTime(createdOrBalledAt).DayNumber;
    }

    public static bool IsAttentionCandidate(
        DoughQualityStatus currentStatus,
        DateTime createdOrBalledAt,
        DateOnly referenceDate,
        DateOnly? mustUseByDate = null)
    {
        if (currentStatus is DoughQualityStatus.Discarded or DoughQualityStatus.Attention)
        {
            return false;
        }

        if (currentStatus == DoughQualityStatus.MustUseNextDay &&
            mustUseByDate.HasValue &&
            referenceDate > mustUseByDate.Value)
        {
            return true;
        }

        var ageDays = CalculateOperationalAgeDays(createdOrBalledAt, referenceDate);
        return currentStatus == DoughQualityStatus.Good && ageDays >= AttentionCandidateMinimumDays;
    }

    public static DateOnly BuildMustUseByDate(DateTime reballDate)
    {
        return DateOnly.FromDateTime(reballDate).AddDays(ReballMustUseOffsetDays);
    }
}
