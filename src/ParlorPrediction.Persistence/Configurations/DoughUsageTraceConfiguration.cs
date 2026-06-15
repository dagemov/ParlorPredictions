using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughUsageTraceConfiguration : IEntityTypeConfiguration<DoughUsageTrace>
{
    public void Configure(EntityTypeBuilder<DoughUsageTrace> builder)
    {
        builder.ToTable("DoughUsageTraces", table =>
        {
            table.HasCheckConstraint("CK_DoughUsageTraces_TrayCount_Positive", "[TrayCount] > 0");
            table.HasCheckConstraint("CK_DoughUsageTraces_BallsPerTray_Positive", "[BallsPerTray] > 0");
            table.HasCheckConstraint("CK_DoughUsageTraces_BallsUsed_MatchesTrays", "[BallsUsed] = ([TrayCount] * [BallsPerTray])");
            table.HasCheckConstraint("CK_DoughUsageTraces_SourceType_Allowed", "[SourceType] <> 'Discarded'");
        });

        builder.HasKey(trace => trace.Id);

        builder.Property(trace => trace.Id)
            .ValueGeneratedNever();

        builder.Property(trace => trace.UsageDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(trace => trace.SourceDoughBatchQualityRecordId)
            .IsRequired();

        builder.Property(trace => trace.SourceDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(trace => trace.SourceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(trace => trace.Destination)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(trace => trace.TrayCount)
            .IsRequired();

        builder.Property(trace => trace.BallsPerTray)
            .IsRequired();

        builder.Property(trace => trace.BallsUsed)
            .IsRequired();

        builder.Property(trace => trace.Notes)
            .HasMaxLength(DoughUsageTrace.NotesMaxLength);

        builder.Property(trace => trace.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(trace => trace.UpdatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(trace => trace.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(trace => trace.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(trace => trace.SourceDoughBatchQualityRecord)
            .WithMany(record => record.UsageTraces)
            .HasForeignKey(trace => trace.SourceDoughBatchQualityRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(trace => trace.CreatedByUser)
            .WithMany()
            .HasForeignKey(trace => trace.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(trace => trace.UpdatedByUser)
            .WithMany()
            .HasForeignKey(trace => trace.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(trace => trace.UsageDate);
        builder.HasIndex(trace => trace.SourceDoughBatchQualityRecordId);
        builder.HasIndex(trace => trace.SourceDate);
        builder.HasIndex(trace => trace.Destination);
    }
}
