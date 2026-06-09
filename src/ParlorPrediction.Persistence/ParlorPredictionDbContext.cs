using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence;

public sealed class ParlorPredictionDbContext : IdentityDbContext<User>
{
    public ParlorPredictionDbContext(DbContextOptions<ParlorPredictionDbContext> options)
        : base(options)
    {
    }

    public DbSet<DoughBatch> DoughBatches => Set<DoughBatch>();

    public DbSet<DoughBatchQualityRecord> DoughBatchQualityRecords => Set<DoughBatchQualityRecord>();

    public DbSet<DoughDemandPlan> DoughDemandPlans => Set<DoughDemandPlan>();

    public DbSet<DoughInventorySnapshot> DoughInventorySnapshots => Set<DoughInventorySnapshot>();

    public DbSet<DoughLossRecord> DoughLossRecords => Set<DoughLossRecord>();

    public DbSet<DoughPrepRecommendation> DoughPrepRecommendations => Set<DoughPrepRecommendation>();

    public DbSet<DoughReballRecord> DoughReballRecords => Set<DoughReballRecord>();

    public DbSet<ManagerPrepRecommendation> ManagerPrepRecommendations => Set<ManagerPrepRecommendation>();

    public DbSet<PrepTask> PrepTasks => Set<PrepTask>();

    public DbSet<PrepItem> PrepItems => Set<PrepItem>();

    public DbSet<PrepStation> PrepStations => Set<PrepStation>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<RestaurantEvent> RestaurantEvents => Set<RestaurantEvent>();

    public DbSet<SalesHistory> SalesHistories => Set<SalesHistory>();

    public DbSet<WeeklyDoughClosing> WeeklyDoughClosings => Set<WeeklyDoughClosing>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Entity<RefreshToken>()
            .HasIndex(token => token.Token)
            .IsUnique();
    }
}
