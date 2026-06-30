using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class ProductionLedgerConfiguration : IEntityTypeConfiguration<ProductionLedger>
{
    public void Configure(EntityTypeBuilder<ProductionLedger> builder)
    {
        builder.ToTable("ProductionLedgers", table =>
        {
            table.HasCheckConstraint(
                "CK_ProductionLedgers_SourceType_NotEmpty",
                "LEN(LTRIM(RTRIM([SourceType]))) > 0");
            table.HasCheckConstraint(
                "CK_ProductionLedgers_TotalBallsCreated_NonNegative",
                "[TotalBallsCreated] >= 0");
            table.HasCheckConstraint(
                "CK_ProductionLedgers_BallsCompleted_NonNegative",
                "[BallsCompleted] >= 0");
            table.HasCheckConstraint(
                "CK_ProductionLedgers_BallsReballed_NonNegative",
                "[BallsReballed] >= 0");
            table.HasCheckConstraint(
                "CK_ProductionLedgers_BallsDiscarded_NonNegative",
                "[BallsDiscarded] >= 0");
        });

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .ValueGeneratedNever();

        builder.Property(entry => entry.OccurredOn)
            .IsRequired();

        builder.Property(entry => entry.SourceType)
            .IsRequired()
            .HasMaxLength(ProductionLedger.SourceTypeMaxLength);

        builder.Property(entry => entry.SourceEntityId)
            .IsRequired();

        builder.Property(entry => entry.TotalBallsCreated)
            .IsRequired();

        builder.Property(entry => entry.BallsCompleted)
            .IsRequired();

        builder.Property(entry => entry.BallsReballed)
            .IsRequired();

        builder.Property(entry => entry.BallsDiscarded)
            .IsRequired();

        builder.Property(entry => entry.Notes)
            .HasMaxLength(ProductionLedger.NotesMaxLength);

        builder.Property(entry => entry.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(entry => entry.OccurredOn);
        builder.HasIndex(entry => new { entry.SourceType, entry.SourceEntityId, entry.CreatedAtUtc });
    }
}
