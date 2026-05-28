using Billing.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.API.Infrastructure.EntityConfigurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", table =>
        {
            table.HasCheckConstraint("CK_Invoices_ElectricityIndex", "\"ElectricityNewIndex\" >= \"ElectricityOldIndex\"");
            table.HasCheckConstraint("CK_Invoices_WaterIndex", "\"WaterNewIndex\" >= \"WaterOldIndex\"");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.BillingMonth).IsRequired();
        builder.Property(i => i.BillingYear).IsRequired();
        builder.Property(i => i.ElectricityTierSnapshot).IsRequired().HasColumnType("jsonb");
        builder.Property(i => i.ElectricityAmount).HasPrecision(18, 2);
        builder.Property(i => i.WaterTierSnapshot).IsRequired().HasColumnType("jsonb");
        builder.Property(i => i.WaterAmount).HasPrecision(18, 2);
        builder.Property(i => i.SurchargeTotal).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        builder.HasIndex(i => new { i.RoomId, i.BillingYear, i.BillingMonth }).IsUnique();
        builder.HasIndex(i => new { i.RoomId, i.BillingYear, i.BillingMonth, i.CreatedAt })
            .IsDescending(false, true, true, true);
        builder.HasIndex(i => new { i.Status, i.BillingYear, i.BillingMonth });

        builder.HasOne(i => i.ContractTemplate)
            .WithMany(t => t.Invoices)
            .HasForeignKey(i => i.ContractTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
