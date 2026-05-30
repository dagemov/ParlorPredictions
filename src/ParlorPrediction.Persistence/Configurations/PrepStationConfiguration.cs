using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Persistence.Seeds;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class PrepStationConfiguration : IEntityTypeConfiguration<PrepStation>
{
    public void Configure(EntityTypeBuilder<PrepStation> builder)
    {
        builder.ToTable("PrepStations");

        builder.HasKey(station => station.Id);

        builder.Property(station => station.Id)
            .ValueGeneratedNever();

        builder.Property(station => station.Name)
            .IsRequired()
            .HasMaxLength(PrepStation.NameMaxLength);

        builder.Property(station => station.Code)
            .IsRequired()
            .HasMaxLength(PrepStation.CodeMaxLength);

        builder.Property(station => station.Description)
            .HasMaxLength(PrepStation.DescriptionMaxLength);

        builder.Property(station => station.IsActive)
            .HasDefaultValue(true);

        builder.Property(station => station.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(station => station.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(station => station.Code)
            .IsUnique();

        builder.Navigation(station => station.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(
            new
            {
                Id = PrepCatalogSeed.PizzaStationId,
                Name = "Pizza",
                Code = "PIZZA",
                Description = "Primary pizza preparation station.",
                IsActive = true,
                CreatedAtUtc = PrepCatalogSeed.SeededAtUtc,
                UpdatedAtUtc = PrepCatalogSeed.SeededAtUtc
            },
            new
            {
                Id = PrepCatalogSeed.BarStationId,
                Name = "Bar",
                Code = "BAR",
                Description = "Bar preparation station.",
                IsActive = true,
                CreatedAtUtc = PrepCatalogSeed.SeededAtUtc,
                UpdatedAtUtc = PrepCatalogSeed.SeededAtUtc
            },
            new
            {
                Id = PrepCatalogSeed.SaladStationId,
                Name = "Salad",
                Code = "SALAD",
                Description = "Salad preparation station.",
                IsActive = true,
                CreatedAtUtc = PrepCatalogSeed.SeededAtUtc,
                UpdatedAtUtc = PrepCatalogSeed.SeededAtUtc
            },
            new
            {
                Id = PrepCatalogSeed.ExpoStationId,
                Name = "Expo",
                Code = "EXPO",
                Description = "Expo preparation station.",
                IsActive = true,
                CreatedAtUtc = PrepCatalogSeed.SeededAtUtc,
                UpdatedAtUtc = PrepCatalogSeed.SeededAtUtc
            },
            new
            {
                Id = PrepCatalogSeed.GeneralStationId,
                Name = "General",
                Code = "GENERAL",
                Description = "General restaurant preparation station.",
                IsActive = true,
                CreatedAtUtc = PrepCatalogSeed.SeededAtUtc,
                UpdatedAtUtc = PrepCatalogSeed.SeededAtUtc
            });
    }
}
