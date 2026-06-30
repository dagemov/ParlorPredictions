using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class OperationalAuditEntryConfiguration : IEntityTypeConfiguration<OperationalAuditEntry>
{
    public void Configure(EntityTypeBuilder<OperationalAuditEntry> builder)
    {
        builder.ToTable("OperationalAuditEntries", table =>
        {
            table.HasCheckConstraint(
                "CK_OperationalAuditEntries_ActionType_NotEmpty",
                "LEN(LTRIM(RTRIM([ActionType]))) > 0");
            table.HasCheckConstraint(
                "CK_OperationalAuditEntries_SourceText_NotEmpty",
                "LEN(LTRIM(RTRIM([SourceText]))) > 0");
            table.HasCheckConstraint(
                "CK_OperationalAuditEntries_ActorUserId_NotEmpty",
                "LEN(LTRIM(RTRIM([ActorUserId]))) > 0");
        });

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .ValueGeneratedNever();

        builder.Property(entry => entry.CorrelationId)
            .IsRequired();

        builder.Property(entry => entry.ActionType)
            .IsRequired()
            .HasMaxLength(OperationalAuditEntry.ActionTypeMaxLength);

        builder.Property(entry => entry.ActorUserId)
            .IsRequired()
            .HasMaxLength(OperationalAuditEntry.UserIdMaxLength);

        builder.Property(entry => entry.SourceText)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(entry => entry.NormalizedIntentJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(entry => entry.BeforeSnapshotJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(entry => entry.AfterPreviewJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(entry => entry.ValidationWarningsJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(entry => entry.DraftId);

        builder.Property(entry => entry.ApprovedEntityId);

        builder.Property(entry => entry.TimestampUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(entry => entry.CorrelationId);
        builder.HasIndex(entry => entry.DraftId);
        builder.HasIndex(entry => entry.TimestampUtc);
    }
}
