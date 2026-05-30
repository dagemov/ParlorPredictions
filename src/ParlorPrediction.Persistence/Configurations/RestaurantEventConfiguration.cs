using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class RestaurantEventConfiguration : IEntityTypeConfiguration<RestaurantEvent>
{
    public void Configure(EntityTypeBuilder<RestaurantEvent> builder)
    {
        builder.ToTable("RestaurantEvents", table =>
        {
            table.HasCheckConstraint("CK_RestaurantEvents_EstimatedPizzas_NonNegative", "[EstimatedPizzas] >= 0");
            table.HasCheckConstraint("CK_RestaurantEvents_EstimatedDoughBalls_NonNegative", "[EstimatedDoughBalls] >= 0");
        });

        builder.HasKey(evt => evt.Id);

        builder.Property(evt => evt.Id)
            .ValueGeneratedNever();

        builder.Property(evt => evt.Name)
            .IsRequired()
            .HasMaxLength(RestaurantEvent.NameMaxLength);

        builder.Property(evt => evt.EventDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(evt => evt.EstimatedPizzas)
            .IsRequired();

        builder.Property(evt => evt.EstimatedDoughBalls)
            .IsRequired();

        builder.Property(evt => evt.AllowShortFermentation)
            .HasDefaultValue(false);

        builder.Property(evt => evt.IsActive)
            .HasDefaultValue(true);

        builder.Property(evt => evt.ExternalCalendarEventId)
            .HasMaxLength(RestaurantEvent.ExternalCalendarEventIdMaxLength);

        builder.Property(evt => evt.Notes)
            .HasMaxLength(RestaurantEvent.NotesMaxLength);

        builder.Property(evt => evt.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(evt => evt.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(evt => new { evt.EventDate, evt.IsActive });
    }
}
