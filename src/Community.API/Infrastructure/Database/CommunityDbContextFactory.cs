using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Community.API.Infrastructure.Database;

public class CommunityDbContextFactory : IDesignTimeDbContextFactory<CommunityDbContext>
{
    public CommunityDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<CommunityDbContextFactory>()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CommunityDbContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("communitydb"));

        return new CommunityDbContext(optionsBuilder.Options);
    }
}