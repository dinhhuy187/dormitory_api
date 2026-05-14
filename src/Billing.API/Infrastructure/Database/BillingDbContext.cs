using Microsoft.EntityFrameworkCore;

namespace Billing.API.Infrastructure.Database;

public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
