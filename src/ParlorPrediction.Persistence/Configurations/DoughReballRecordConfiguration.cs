using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughReballRecordConfiguration : IEntityTypeConfiguration<DoughReballRecord>
{
    public void Configure(EntityTypeBuilder<DoughReballRecord> builder)
    {
        builder.ToTable("DoughReballRecords", table =>
        {
            table.HasCheckConstraint("CK_DoughReballRecords_QuantityBeforeReball_Positive", "[QuantityBeforeReball] > 0");
            table.HasCheckConstraint("CK_DoughReballRecords_QuantityRecoveredBalls_NonNegative", "[QuantityRecoveredBalls] >= 0");
            table.HasCheckConstraint("CK_DoughReballRecords_QuantityLostBalls_NonNegative", "[QuantityLostBalls] >= 0");
            table.HasCheckConstraint("CK_DoughReballRecords_RecoveredNotGreaterThanBefore", "[QuantityBeforeReball] >= [QuantityRecoveredBalls]");
            table.HasCheckConstraint("CK_DoughReballRecords_LossMath", "[QuantityLostBalls] = [QuantityBeforeReball] - [QuantityRecoveredBalls]");
            table.HasCheckConstraint(
                "CK_DoughReballRecords_MustUseByDate",
                "([Result] <> 'PartialRecovered' OR [MustUseByDate] IS NOT NULL)");
        });

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .ValueGeneratedNever();

        builder.Property(record => record.DoughBatchQualityRecordId)
            .IsRequired();

        builder.Property(record => record.QuantityBeforeReball)
            .IsRequired();

        builder.Property(record => record.QuantityRecoveredBalls)
            .IsRequired();

        builder.Property(record => record.QuantityLostBalls)
            .IsRequired();

        builder.Property(record => record.ReballDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(record => record.Result)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(record => record.MustUseByDate)
            .HasColumnType("date");

        builder.Property(record => record.ManagerNote)
            .HasMaxLength(DoughReballRecord.ManagerNoteMaxLength);

        builder.Property(record => record.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(record => record.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(record => record.CreatedByUser)
            .WithMany()
            .HasForeignKey(record => record.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(record => record.DoughBatchQualityRecordId);
        builder.HasIndex(record => record.ReballDate);
        builder.HasIndex(record => record.Result);
    }
}
