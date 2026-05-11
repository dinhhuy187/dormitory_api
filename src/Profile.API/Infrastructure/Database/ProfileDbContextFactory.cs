using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Profile.API.Infrastructure.Database;

public class ProfileDbContextFactory : IDesignTimeDbContextFactory<ProfileDbContext>
{
    public ProfileDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<ProfileDbContextFactory>()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ProfileDbContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("profiledb"));

        return new ProfileDbContext(optionsBuilder.Options);
    }
}