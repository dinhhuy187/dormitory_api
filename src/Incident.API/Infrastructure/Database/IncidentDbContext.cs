using Incident.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Incident.API.Infrastructure.Database;

public class IncidentDbContext(DbContextOptions<IncidentDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Incident> Incidents => Set<Domain.Entities.Incident>();
    public DbSet<IncidentCategory> IncidentCategories => Set<IncidentCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IncidentDbContext).Assembly);
    }
}