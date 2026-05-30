using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Persistence.Seeds;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class PrepItemConfiguration : IEntityTypeConfiguration<PrepItem>
{
    public void Configure(EntityTypeBuilder<PrepItem> builder)
    {
        builder.ToTable("PrepItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedNever();

        builder.Property(item => item.PrepStationId)
            .IsRequired();

        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(PrepItem.NameMaxLength);

        builder.Property(item => item.Code)
            .IsRequired()
            .HasMaxLength(PrepItem.CodeMaxLength);

        builder.Property(item => item.Description)
            .HasMaxLength(PrepItem.DescriptionMaxLength);

        builder.Property(item => item.IsActive)
            .HasDefaultValue(true);

        builder.Property(item => item.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(item => item.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(item => item.Code)
            .IsUnique();

        builder.HasOne(item => item.PrepStation)
            .WithMany(station => station.Items)
            .HasForeignKey(item => item.PrepStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(new
        {
            Id = PrepCatalogSeed.DoughItemId,
            PrepStationId = PrepCatalogSeed.PizzaStationId,
            Name = "Dough",
            Code = "DOUGH",
            Description = "Base dough preparation item for pizza service.",
            IsActive = true,
            CreatedAtUtc = PrepCatalogSeed.SeededAtUtc,
            UpdatedAtUtc = PrepCatalogSeed.SeededAtUtc
        });
    }
}
