using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Incident.API.Infrastructure.Database;

public class IncidentDbContextFactory : IDesignTimeDbContextFactory<IncidentDbContext>
{
    public IncidentDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<IncidentDbContextFactory>()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<IncidentDbContext>();

        optionsBuilder.UseNpgsql(
            config.GetConnectionString("incidentdb")
        );

        return new IncidentDbContext(optionsBuilder.Options);
    }
}