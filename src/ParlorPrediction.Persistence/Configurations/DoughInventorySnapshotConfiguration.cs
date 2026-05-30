using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughInventorySnapshotConfiguration : IEntityTypeConfiguration<DoughInventorySnapshot>
{
    public void Configure(EntityTypeBuilder<DoughInventorySnapshot> builder)
    {
        builder.ToTable("DoughInventorySnapshots", table =>
        {
            table.HasCheckConstraint("CK_DoughInventorySnapshots_AvailableBalls_NonNegative", "[AvailableBalls] >= 0");
            table.HasCheckConstraint("CK_DoughInventorySnapshots_NewBalls_NonNegative", "[NewBalls] >= 0");
            table.HasCheckConstraint("CK_DoughInventorySnapshots_OldBalls_NonNegative", "[OldBalls] >= 0");
            table.HasCheckConstraint("CK_DoughInventorySnapshots_ReservedBalls_NonNegative", "[ReservedBalls] >= 0");
            table.HasCheckConstraint("CK_DoughInventorySnapshots_UsedBalls_NonNegative", "[UsedBalls] >= 0");
            table.HasCheckConstraint("CK_DoughInventorySnapshots_WasteBalls_NonNegative", "[WasteBalls] >= 0");
        });

        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.Id)
            .ValueGeneratedNever();

        builder.Property(snapshot => snapshot.SnapshotDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(snapshot => snapshot.AvailableBalls)
            .IsRequired();

        builder.Property(snapshot => snapshot.NewBalls)
            .IsRequired();

        builder.Property(snapshot => snapshot.OldBalls)
            .IsRequired();

        builder.Property(snapshot => snapshot.ReservedBalls)
            .IsRequired();

        builder.Property(snapshot => snapshot.UsedBalls)
            .IsRequired();

        builder.Property(snapshot => snapshot.WasteBalls)
            .IsRequired();

        builder.Property(snapshot => snapshot.Notes)
            .HasMaxLength(DoughInventorySnapshot.NotesMaxLength);

        builder.Property(snapshot => snapshot.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(snapshot => snapshot.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
