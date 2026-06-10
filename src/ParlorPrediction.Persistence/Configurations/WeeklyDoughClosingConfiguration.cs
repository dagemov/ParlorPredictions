using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class WeeklyDoughClosingConfiguration : IEntityTypeConfiguration<WeeklyDoughClosing>
{
    public void Configure(EntityTypeBuilder<WeeklyDoughClosing> builder)
    {
        builder.ToTable("WeeklyDoughClosings", table =>
        {
            table.HasCheckConstraint("CK_WeeklyDoughClosings_WeekWindow", "DATEDIFF(day, [WeekStartDate], [WeekEndDate]) = 5");
            table.HasCheckConstraint("CK_WeeklyDoughClosings_NeededBalls_NonNegative", "[NeededBalls] >= 0");
            table.HasCheckConstraint("CK_WeeklyDoughClosings_ProducedBalls_NonNegative", "[ProducedBalls] >= 0");
            table.HasCheckConstraint("CK_WeeklyDoughClosings_UsedBalls_NonNegative", "[UsedBalls] >= 0");
            table.HasCheckConstraint("CK_WeeklyDoughClosings_LostBalls_NonNegative", "[LostBalls] >= 0");
            table.HasCheckConstraint("CK_WeeklyDoughClosings_LeftoverReadyBalls_NonNegative", "[LeftoverReadyBalls] >= 0");
            table.HasCheckConstraint("CK_WeeklyDoughClosings_LeftoverAttentionBalls_NonNegative", "[LeftoverAttentionBalls] >= 0");
            table.HasCheckConstraint("CK_WeeklyDoughClosings_LeftoverMixedLoads_NonNegative", "[LeftoverMixedLoads] >= 0");
        });

        builder.HasKey(closing => closing.Id);

        builder.Property(closing => closing.Id)
            .ValueGeneratedNever();

        builder.Property(closing => closing.WeekStartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(closing => closing.WeekEndDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(closing => closing.NeededBalls)
            .IsRequired();

        builder.Property(closing => closing.ProducedBalls)
            .IsRequired();

        builder.Property(closing => closing.UsedBalls)
            .IsRequired();

        builder.Property(closing => closing.LostBalls)
            .IsRequired();

        builder.Property(closing => closing.LeftoverReadyBalls)
            .IsRequired();

        builder.Property(closing => closing.LeftoverAttentionBalls)
            .IsRequired();

        builder.Property(closing => closing.LeftoverMixedLoads)
            .IsRequired();

        builder.Property(closing => closing.Notes)
            .HasMaxLength(WeeklyDoughClosing.NotesMaxLength);

        builder.Property(closing => closing.ClosedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(closing => closing.ClosedAtUtc)
            .IsRequired();

        builder.Property(closing => closing.CorrectedByUserId)
            .HasMaxLength(450);

        builder.Property(closing => closing.CorrectedAtUtc);

        builder.Property(closing => closing.CorrectionNote)
            .HasMaxLength(WeeklyDoughClosing.CorrectionNoteMaxLength);

        builder.Property(closing => closing.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(closing => closing.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(closing => closing.ClosedByUser)
            .WithMany()
            .HasForeignKey(closing => closing.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(closing => closing.CorrectedByUser)
            .WithMany()
            .HasForeignKey(closing => closing.CorrectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(closing => closing.WeekStartDate)
            .IsUnique();

        builder.HasIndex(closing => closing.ClosedAtUtc);
    }
}
