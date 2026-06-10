using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DailyDoughClosingConfiguration : IEntityTypeConfiguration<DailyDoughClosing>
{
    public void Configure(EntityTypeBuilder<DailyDoughClosing> builder)
    {
        builder.ToTable("DailyDoughClosings", table =>
        {
            table.HasCheckConstraint("CK_DailyDoughClosings_ForecastNeededBalls_NonNegative", "[ForecastNeededBalls] >= 0");
            table.HasCheckConstraint("CK_DailyDoughClosings_ActualUsedBalls_NonNegative", "[ActualUsedBalls] >= 0");
        });

        builder.HasKey(closing => closing.Id);

        builder.Property(closing => closing.Id)
            .ValueGeneratedNever();

        builder.Property(closing => closing.ClosingDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(closing => closing.WeekStartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(closing => closing.ForecastNeededBalls)
            .IsRequired();

        builder.Property(closing => closing.ActualUsedBalls)
            .IsRequired();

        builder.Ignore(closing => closing.DailyVariance);

        builder.Property(closing => closing.Notes)
            .HasMaxLength(DailyDoughClosing.NotesMaxLength);

        builder.Property(closing => closing.ClosedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(closing => closing.ClosedAtUtc)
            .IsRequired();

        builder.Property(closing => closing.CorrectedByUserId)
            .HasMaxLength(450);

        builder.Property(closing => closing.CorrectedAtUtc);

        builder.Property(closing => closing.CorrectionNote)
            .HasMaxLength(DailyDoughClosing.CorrectionNoteMaxLength);

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

        builder.HasIndex(closing => closing.ClosingDate)
            .IsUnique();

        builder.HasIndex(closing => closing.WeekStartDate);

        builder.HasIndex(closing => closing.ClosedAtUtc);
    }
}
