using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParlorPrediction.Domain.Entities;

namespace ParlorPrediction.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.ProfileImageUrl)
            .HasMaxLength(2048);

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(user => user.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(user => user.UpdatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
