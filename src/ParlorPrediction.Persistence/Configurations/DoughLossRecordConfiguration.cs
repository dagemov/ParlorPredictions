using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughLossRecordConfiguration : IEntityTypeConfiguration<DoughLossRecord>
{
    public void Configure(EntityTypeBuilder<DoughLossRecord> builder)
    {
        builder.ToTable("DoughLossRecords", table =>
        {
            table.HasCheckConstraint("CK_DoughLossRecords_QuantityLostBalls_Positive", "[QuantityLostBalls] > 0");
        });

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .ValueGeneratedNever();

        builder.Property(record => record.DoughBatchQualityRecordId)
            .IsRequired();

        builder.Property(record => record.QuantityLostBalls)
            .IsRequired();

        builder.Property(record => record.LossReason)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(record => record.LossDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(record => record.ManagerNote)
            .HasMaxLength(DoughLossRecord.ManagerNoteMaxLength);

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
        builder.HasIndex(record => record.LossDate);
        builder.HasIndex(record => record.LossReason);
    }
}
