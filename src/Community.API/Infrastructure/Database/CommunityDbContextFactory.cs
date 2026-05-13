using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Community.API.Infrastructure.Database;

public class CommunityDbContextFactory : IDesignTimeDbContextFactory<CommunityDbContext>
{
    public CommunityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CommunityDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=communitydb;Username=postgres;Password=postgres");
        return new CommunityDbContext(optionsBuilder.Options);
    }
}