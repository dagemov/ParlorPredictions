using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughBatchConfiguration : IEntityTypeConfiguration<DoughBatch>
{
    public void Configure(EntityTypeBuilder<DoughBatch> builder)
    {
        builder.ToTable("DoughBatches", table =>
        {
            table.HasCheckConstraint("CK_DoughBatches_TotalCases_Positive", "[TotalCases] > 0");
            table.HasCheckConstraint("CK_DoughBatches_BallsPerCase_Positive", "[BallsPerCase] > 0");
            table.HasCheckConstraint("CK_DoughBatches_TotalBalls_Positive", "[TotalBalls] > 0");
            table.HasCheckConstraint(
                "CK_DoughBatches_BalledState",
                "([IsBalled] = 0 AND [BalledAtUtc] IS NULL) OR ([IsBalled] = 1 AND [BalledAtUtc] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_DoughBatches_FermentationReadyDate",
                "DATEDIFF(day, [BatchDate], [FermentationReadyDate]) >= 2");
        });

        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.Id)
            .ValueGeneratedNever();

        builder.Property(batch => batch.BatchDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(batch => batch.TotalCases)
            .IsRequired();

        builder.Property(batch => batch.BallsPerCase)
            .IsRequired()
            .HasDefaultValue(DoughBatch.DefaultBallsPerCase);

        builder.Property(batch => batch.TotalBalls)
            .IsRequired();

        builder.Property(batch => batch.FermentationReadyDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(batch => batch.IsBalled)
            .HasDefaultValue(false);

        builder.Property(batch => batch.BalledAtUtc);

        builder.Property(batch => batch.IsEventException)
            .HasDefaultValue(false);

        builder.Property(batch => batch.Notes)
            .HasMaxLength(DoughBatch.NotesMaxLength);

        builder.Property(batch => batch.IsVoided)
            .HasDefaultValue(false);

        builder.Property(batch => batch.VoidedAtUtc);

        builder.Property(batch => batch.VoidReason)
            .HasMaxLength(DoughBatch.NotesMaxLength);

        builder.Property(batch => batch.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(batch => batch.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
