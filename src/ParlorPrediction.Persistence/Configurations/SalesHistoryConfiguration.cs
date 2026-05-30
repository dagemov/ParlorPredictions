using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class SalesHistoryConfiguration : IEntityTypeConfiguration<SalesHistory>
{
    public void Configure(EntityTypeBuilder<SalesHistory> builder)
    {
        builder.ToTable("SalesHistories", table =>
        {
            table.HasCheckConstraint("CK_SalesHistories_QuantitySold_NonNegative", "[QuantitySold] >= 0");
            table.HasCheckConstraint("CK_SalesHistories_DoughBallsUsed_NonNegative", "[DoughBallsUsed] >= 0");
        });

        builder.HasKey(sale => sale.Id);

        builder.Property(sale => sale.Id)
            .ValueGeneratedNever();

        builder.Property(sale => sale.SaleDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(sale => sale.DayOfWeek)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(sale => sale.ProductName)
            .IsRequired()
            .HasMaxLength(SalesHistory.ProductNameMaxLength);

        builder.Property(sale => sale.QuantitySold)
            .IsRequired();

        builder.Property(sale => sale.DoughBallsUsed)
            .IsRequired();

        builder.Property(sale => sale.Notes)
            .HasMaxLength(SalesHistory.NotesMaxLength);

        builder.Property(sale => sale.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(sale => sale.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(sale => sale.SaleDate);

        builder.HasIndex(sale => sale.ProductName);
    }
}
