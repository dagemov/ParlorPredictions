using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class OperationalDraftConfiguration : IEntityTypeConfiguration<OperationalDraft>
{
    public void Configure(EntityTypeBuilder<OperationalDraft> builder)
    {
        builder.ToTable("OperationalDrafts", table =>
        {
            table.HasCheckConstraint(
                "CK_OperationalDrafts_DraftType_NotEmpty",
                "LEN(LTRIM(RTRIM([DraftType]))) > 0");
            table.HasCheckConstraint(
                "CK_OperationalDrafts_SourceText_NotEmpty",
                "LEN(LTRIM(RTRIM([SourceText]))) > 0");
            table.HasCheckConstraint(
                "CK_OperationalDrafts_CreatedBy_NotEmpty",
                "LEN(LTRIM(RTRIM([CreatedBy]))) > 0");
        });

        builder.HasKey(draft => draft.Id);

        builder.Property(draft => draft.Id)
            .ValueGeneratedNever();

        builder.Property(draft => draft.CorrelationId)
            .IsRequired();

        builder.Property(draft => draft.DraftType)
            .IsRequired()
            .HasMaxLength(OperationalDraft.DraftTypeMaxLength);

        builder.Property(draft => draft.SourceText)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(draft => draft.NormalizedIntentJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(draft => draft.BeforeSnapshotJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(draft => draft.AfterPreviewJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(draft => draft.ValidationWarningsJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(draft => draft.DraftPayloadJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(draft => draft.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(OperationalDraftStatus.Pending);

        builder.Property(draft => draft.CreatedBy)
            .IsRequired()
            .HasMaxLength(OperationalDraft.UserIdMaxLength);

        builder.Property(draft => draft.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(draft => draft.ReviewedByUserId)
            .HasMaxLength(OperationalDraft.UserIdMaxLength);

        builder.Property(draft => draft.ReviewedAtUtc);

        builder.Property(draft => draft.StatusReason)
            .HasMaxLength(OperationalDraft.StatusReasonMaxLength);

        builder.Property(draft => draft.ApprovedEntityId);

        builder.HasIndex(draft => draft.CorrelationId);
        builder.HasIndex(draft => draft.Status);
        builder.HasIndex(draft => draft.CreatedAtUtc);
    }
}
