using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughPrepRecommendationConfiguration : IEntityTypeConfiguration<DoughPrepRecommendation>
{
    public void Configure(EntityTypeBuilder<DoughPrepRecommendation> builder)
    {
        builder.ToTable("DoughPrepRecommendations", table =>
        {
            table.HasCheckConstraint("CK_DoughPrepRecommendations_RequiredBalls_NonNegative", "[RequiredBalls] >= 0");
            table.HasCheckConstraint("CK_DoughPrepRecommendations_HistoricalAverageBalls_NonNegative", "[HistoricalAverageBalls] >= 0");
            table.HasCheckConstraint("CK_DoughPrepRecommendations_EventEstimatedBalls_NonNegative", "[EventEstimatedBalls] >= 0");
            table.HasCheckConstraint("CK_DoughPrepRecommendations_AvailableBalls_NonNegative", "[AvailableBalls] >= 0");
            table.HasCheckConstraint("CK_DoughPrepRecommendations_MissingBalls_NonNegative", "[MissingBalls] >= 0");
            table.HasCheckConstraint("CK_DoughPrepRecommendations_RecommendedCases_NonNegative", "[RecommendedCases] >= 0");
            table.HasCheckConstraint("CK_DoughPrepRecommendations_RecommendedLoads_NonNegative", "[RecommendedLoads] >= 0");
        });

        builder.HasKey(recommendation => recommendation.Id);

        builder.Property(recommendation => recommendation.Id)
            .ValueGeneratedNever();

        builder.Property(recommendation => recommendation.RecommendationDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(recommendation => recommendation.RequiredBalls)
            .IsRequired();

        builder.Property(recommendation => recommendation.HistoricalAverageBalls)
            .IsRequired();

        builder.Property(recommendation => recommendation.EventEstimatedBalls)
            .IsRequired();

        builder.Property(recommendation => recommendation.AvailableBalls)
            .IsRequired();

        builder.Property(recommendation => recommendation.MissingBalls)
            .IsRequired();

        builder.Property(recommendation => recommendation.RecommendedCases)
            .IsRequired();

        builder.Property(recommendation => recommendation.RecommendedLoads)
            .IsRequired();

        builder.Property(recommendation => recommendation.ShouldMakeDough)
            .IsRequired();

        builder.Property(recommendation => recommendation.ShouldBallDough)
            .IsRequired();

        builder.Property(recommendation => recommendation.UsesShortFermentationException)
            .IsRequired();

        builder.Property(recommendation => recommendation.Reason)
            .IsRequired()
            .HasMaxLength(DoughPrepRecommendation.ReasonMaxLength);

        builder.Property(recommendation => recommendation.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(recommendation => recommendation.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(recommendation => recommendation.RecommendationDate);
    }
}
