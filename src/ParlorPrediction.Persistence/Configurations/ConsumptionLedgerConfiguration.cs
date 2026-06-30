using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class ConsumptionLedgerConfiguration : IEntityTypeConfiguration<ConsumptionLedger>
{
    public void Configure(EntityTypeBuilder<ConsumptionLedger> builder)
    {
        builder.ToTable("ConsumptionLedgers", table =>
        {
            table.HasCheckConstraint(
                "CK_ConsumptionLedgers_SourceType_NotEmpty",
                "LEN(LTRIM(RTRIM([SourceType]))) > 0");
            table.HasCheckConstraint(
                "CK_ConsumptionLedgers_SalesBalls_NonNegative",
                "[SalesBalls] >= 0");
            table.HasCheckConstraint(
                "CK_ConsumptionLedgers_EventBalls_NonNegative",
                "[EventBalls] >= 0");
            table.HasCheckConstraint(
                "CK_ConsumptionLedgers_ServiceUsageBalls_NonNegative",
                "[ServiceUsageBalls] >= 0");
        });

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .ValueGeneratedNever();

        builder.Property(entry => entry.OccurredOn)
            .IsRequired();

        builder.Property(entry => entry.SourceType)
            .IsRequired()
            .HasMaxLength(ConsumptionLedger.SourceTypeMaxLength);

        builder.Property(entry => entry.SourceEntityId)
            .IsRequired();

        builder.Property(entry => entry.SalesBalls)
            .IsRequired();

        builder.Property(entry => entry.EventBalls)
            .IsRequired();

        builder.Property(entry => entry.ServiceUsageBalls)
            .IsRequired();

        builder.Property(entry => entry.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(entry => entry.Notes)
            .HasMaxLength(ConsumptionLedger.NotesMaxLength);

        builder.Property(entry => entry.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(entry => entry.OccurredOn);
        builder.HasIndex(entry => new { entry.SourceType, entry.SourceEntityId, entry.CreatedAtUtc });
    }
}
