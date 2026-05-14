using Community.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Community.API.Infrastructure.Database;

public class CommunityDbContext(DbContextOptions<CommunityDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityDbContext).Assembly);
    }
}