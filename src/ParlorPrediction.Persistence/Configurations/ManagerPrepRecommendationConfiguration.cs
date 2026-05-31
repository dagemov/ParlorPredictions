using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class ManagerPrepRecommendationConfiguration : IEntityTypeConfiguration<ManagerPrepRecommendation>
{
    public void Configure(EntityTypeBuilder<ManagerPrepRecommendation> builder)
    {
        builder.ToTable("ManagerPrepRecommendations", table =>
        {
            table.HasCheckConstraint(
                "CK_ManagerPrepRecommendations_RecommendedBalls_NonNegative",
                "[RecommendedBalls] >= 0");
            table.HasCheckConstraint(
                "CK_ManagerPrepRecommendations_RecommendedCases_NonNegative",
                "[RecommendedCases] >= 0");
            table.HasCheckConstraint(
                "CK_ManagerPrepRecommendations_RecommendedLoads_NonNegative",
                "[RecommendedLoads] >= 0");
        });

        builder.HasKey(recommendation => recommendation.Id);

        builder.Property(recommendation => recommendation.Id)
            .ValueGeneratedNever();

        builder.Property(recommendation => recommendation.RecommendationDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(recommendation => recommendation.PrepItemId)
            .IsRequired();

        builder.Property(recommendation => recommendation.RecommendationText)
            .IsRequired()
            .HasMaxLength(ManagerPrepRecommendation.RecommendationTextMaxLength);

        builder.Property(recommendation => recommendation.RecommendedBalls)
            .IsRequired();

        builder.Property(recommendation => recommendation.RecommendedCases)
            .IsRequired();

        builder.Property(recommendation => recommendation.RecommendedLoads)
            .IsRequired();

        builder.Property(recommendation => recommendation.Reason)
            .IsRequired()
            .HasMaxLength(ManagerPrepRecommendation.ReasonMaxLength);

        builder.Property(recommendation => recommendation.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(recommendation => recommendation.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(recommendation => recommendation.PrepItem)
            .WithMany()
            .HasForeignKey(recommendation => recommendation.PrepItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(recommendation => recommendation.CreatedByUser)
            .WithMany()
            .HasForeignKey(recommendation => recommendation.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(recommendation => recommendation.RecommendationDate);
        builder.HasIndex(recommendation => recommendation.PrepItemId);
        builder.HasIndex(recommendation => recommendation.CreatedByUserId);
    }
}
