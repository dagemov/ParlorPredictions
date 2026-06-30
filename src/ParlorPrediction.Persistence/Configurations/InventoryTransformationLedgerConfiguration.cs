using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class InventoryTransformationLedgerConfiguration : IEntityTypeConfiguration<InventoryTransformationLedger>
{
    public void Configure(EntityTypeBuilder<InventoryTransformationLedger> builder)
    {
        builder.ToTable("InventoryTransformationLedgers", table =>
        {
            table.HasCheckConstraint(
                "CK_InventoryTransformationLedgers_SourceType_NotEmpty",
                "LEN(LTRIM(RTRIM([SourceType]))) > 0");
            table.HasCheckConstraint(
                "CK_InventoryTransformationLedgers_BallsRecovered_NonNegative",
                "[BallsRecovered] >= 0");
            table.HasCheckConstraint(
                "CK_InventoryTransformationLedgers_BallsDiscarded_NonNegative",
                "[BallsDiscarded] >= 0");
            table.HasCheckConstraint(
                "CK_InventoryTransformationLedgers_BallsReclassified_NonNegative",
                "[BallsReclassified] >= 0");
        });

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .ValueGeneratedNever();

        builder.Property(entry => entry.OccurredOn)
            .IsRequired();

        builder.Property(entry => entry.SourceType)
            .IsRequired()
            .HasMaxLength(InventoryTransformationLedger.SourceTypeMaxLength);

        builder.Property(entry => entry.SourceEntityId)
            .IsRequired();

        builder.Property(entry => entry.BallsRecovered)
            .IsRequired();

        builder.Property(entry => entry.BallsDiscarded)
            .IsRequired();

        builder.Property(entry => entry.BallsReclassified)
            .IsRequired();

        builder.Property(entry => entry.Notes)
            .HasMaxLength(InventoryTransformationLedger.NotesMaxLength);

        builder.Property(entry => entry.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(entry => entry.OccurredOn);
        builder.HasIndex(entry => new { entry.SourceType, entry.SourceEntityId, entry.CreatedAtUtc });
    }
}
