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

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Entity<RefreshToken>()
            .HasIndex(token => token.Token)
            .IsUnique();
    }
}
