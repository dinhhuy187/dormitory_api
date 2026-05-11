using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Profile.API.Domain.Entities;

namespace Profile.API.Infrastructure.EntityConfigurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfile");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.FullName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Bio).HasMaxLength(500);
        builder.HasIndex(p => p.Id);
    }
}