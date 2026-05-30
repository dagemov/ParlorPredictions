using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;
using ParlorPrediction.Persistence.Seeds;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class DoughDemandPlanConfiguration : IEntityTypeConfiguration<DoughDemandPlan>
{
    public void Configure(EntityTypeBuilder<DoughDemandPlan> builder)
    {
        builder.ToTable("DoughDemandPlans", table =>
        {
            table.HasCheckConstraint("CK_DoughDemandPlans_MinDoughBalls_NonNegative", "[MinDoughBalls] >= 0");
            table.HasCheckConstraint("CK_DoughDemandPlans_MaxDoughBalls_NonNegative", "[MaxDoughBalls] >= 0");
            table.HasCheckConstraint("CK_DoughDemandPlans_MaxGreaterThanMin", "[MaxDoughBalls] >= [MinDoughBalls]");
        });

        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Id)
            .ValueGeneratedNever();

        builder.Property(plan => plan.DayOfWeek)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(plan => plan.SourceName)
            .IsRequired()
            .HasMaxLength(DoughDemandPlan.SourceNameMaxLength);

        builder.Property(plan => plan.MinDoughBalls)
            .IsRequired();

        builder.Property(plan => plan.MaxDoughBalls)
            .IsRequired();

        builder.Property(plan => plan.Notes)
            .HasMaxLength(DoughDemandPlan.NotesMaxLength);

        builder.Property(plan => plan.IsActive)
            .HasDefaultValue(true);

        builder.Property(plan => plan.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(plan => plan.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(plan => new { plan.DayOfWeek, plan.IsActive });

        builder.HasIndex(plan => new { plan.DayOfWeek, plan.SourceName })
            .IsUnique();

        builder.HasData(DoughDemandPlanSeed.Values);
    }
}
