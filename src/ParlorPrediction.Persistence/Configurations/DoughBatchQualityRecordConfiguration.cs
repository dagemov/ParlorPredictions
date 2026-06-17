using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughBatchQualityRecordConfiguration : IEntityTypeConfiguration<DoughBatchQualityRecord>
{
    public void Configure(EntityTypeBuilder<DoughBatchQualityRecord> builder)
    {
        builder.ToTable("DoughBatchQualityRecords", table =>
        {
            table.HasCheckConstraint("CK_DoughBatchQualityRecords_QuantityBalls_Positive", "[QuantityBalls] > 0");
            table.HasCheckConstraint(
                "CK_DoughBatchQualityRecords_DiscardState",
                "([CurrentStatus] <> 'Discarded' OR ([DiscardedAt] IS NOT NULL AND [DiscardReason] IS NOT NULL))");
            table.HasCheckConstraint(
                "CK_DoughBatchQualityRecords_MustUseState",
                "([CurrentStatus] <> 'MustUseNextDay' OR [MustUseByDate] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_DoughBatchQualityRecords_AttentionState",
                "([CurrentStatus] <> 'Attention' OR [AttentionMarkedAt] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_DoughBatchQualityRecords_ReballedState",
                "([CurrentStatus] NOT IN ('Reballed', 'MustUseNextDay') OR [ReballedAt] IS NOT NULL)");
        });

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .ValueGeneratedNever();

        builder.Property(record => record.SourceDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(record => record.OriginalDoughTaskId);

        builder.Property(record => record.CreatedOrBalledAt)
            .IsRequired();

        builder.Property(record => record.QuantityBalls)
            .IsRequired();

        builder.Property(record => record.CurrentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(record => record.StatusReason)
            .HasMaxLength(DoughBatchQualityRecord.StatusReasonMaxLength);

        builder.Property(record => record.AttentionMarkedAt);

        builder.Property(record => record.ReballedAt);

        builder.Property(record => record.MustUseByDate)
            .HasColumnType("date");

        builder.Property(record => record.DiscardedAt);

        builder.Property(record => record.DiscardReason)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(record => record.ManagerNote)
            .HasMaxLength(DoughBatchQualityRecord.ManagerNoteMaxLength);

        builder.Property(record => record.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(record => record.UpdatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(record => record.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(record => record.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(record => record.OriginalDoughTask)
            .WithMany()
            .HasForeignKey(record => record.OriginalDoughTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.CreatedByUser)
            .WithMany()
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.UpdatedByUser)
            .WithMany()
            .HasForeignKey(record => record.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(record => record.LossRecords)
            .WithOne(loss => loss.DoughBatchQualityRecord)
            .HasForeignKey(loss => loss.DoughBatchQualityRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(record => record.ReballRecords)
            .WithOne(reball => reball.DoughBatchQualityRecord)
            .HasForeignKey(reball => reball.DoughBatchQualityRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(record => record.UsageTraces)
            .WithOne(trace => trace.SourceDoughBatchQualityRecord)
            .HasForeignKey(trace => trace.SourceDoughBatchQualityRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(record => record.SourceDate);
        builder.HasIndex(record => record.CreatedOrBalledAt);
        builder.HasIndex(record => record.CurrentStatus);
        builder.HasIndex(record => record.ReballedAt);
        builder.HasIndex(record => record.MustUseByDate);
    }
}
