using Microsoft.EntityFrameworkCore;
using Profile.API.Models;

namespace Profile.API.Data;

public class ProfileDbContext : DbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique(); // 1 user = 1 profile
            entity.Property(e => e.Gender)
                  .HasConversion<string>(); // lưu dạng string
        });
    }
}