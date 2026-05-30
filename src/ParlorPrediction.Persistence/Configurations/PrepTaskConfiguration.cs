using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Domain.Enums;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class PrepTaskConfiguration : IEntityTypeConfiguration<PrepTask>
{
    public void Configure(EntityTypeBuilder<PrepTask> builder)
    {
        builder.ToTable("PrepTasks", table =>
        {
            table.HasCheckConstraint("CK_PrepTasks_QuantityRecommended_NonNegative", "[QuantityRecommended] >= 0");
            table.HasCheckConstraint("CK_PrepTasks_QuantityCompleted_NonNegative", "[QuantityCompleted] >= 0");
            table.HasCheckConstraint(
                "CK_PrepTasks_CompletionState",
                "([Status] <> 'Completed' AND [CompletedAtUtc] IS NULL AND [CompletedByUserId] IS NULL) OR ([Status] = 'Completed' AND [CompletedAtUtc] IS NOT NULL AND [CompletedByUserId] IS NOT NULL AND [QuantityCompleted] > 0)");
        });

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .ValueGeneratedNever();

        builder.Property(task => task.TaskDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(task => task.PrepItemId)
            .IsRequired();

        builder.Property(task => task.PrepStationId)
            .IsRequired();

        builder.Property(task => task.DoughPrepRecommendationId);

        builder.Property(task => task.AssignedRole)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(task => task.QuantityRecommended)
            .IsRequired();

        builder.Property(task => task.QuantityCompleted)
            .IsRequired();

        builder.Property(task => task.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(task => task.CompletedByUserId)
            .HasMaxLength(450);

        builder.Property(task => task.Notes)
            .HasMaxLength(PrepTask.NotesMaxLength);

        builder.Property(task => task.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(task => task.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(task => task.PrepItem)
            .WithMany()
            .HasForeignKey(task => task.PrepItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.PrepStation)
            .WithMany()
            .HasForeignKey(task => task.PrepStationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.DoughPrepRecommendation)
            .WithMany()
            .HasForeignKey(task => task.DoughPrepRecommendationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.CompletedByUser)
            .WithMany()
            .HasForeignKey(task => task.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(task => task.TaskDate);
        builder.HasIndex(task => task.Status);
        builder.HasIndex(task => task.AssignedRole);

        builder.HasIndex(task => task.DoughPrepRecommendationId)
            .IsUnique()
            .HasFilter("[DoughPrepRecommendationId] IS NOT NULL");
    }
}
