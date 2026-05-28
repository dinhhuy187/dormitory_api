using Billing.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Billing.API.Infrastructure.Database;

public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Surcharge> Surcharges { get; set; }
    public DbSet<ServicePriceTier> ServicePriceTiers { get; set; }
    public DbSet<ContractTemplate> ContractTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
