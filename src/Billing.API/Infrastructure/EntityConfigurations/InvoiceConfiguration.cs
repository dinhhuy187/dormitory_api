using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
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
            table.HasCheckConstraint("CK_Invoices_Floor", "\"Floor\" IS NULL OR \"Floor\" > 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceType).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(i => i.TermName).HasMaxLength(100);
        builder.Property(i => i.Description).HasMaxLength(500);
        builder.Property(i => i.BuildingCode).HasMaxLength(50);
        builder.Property(i => i.Floor);
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

        builder.HasIndex(i => new { i.RoomId, i.BillingYear, i.BillingMonth, i.InvoiceType })
            .IsUnique()
            .HasFilter("\"InvoiceType\" = 'MonthlyUtility'");
        builder.HasIndex(i => i.BookingId)
            .IsUnique()
            .HasFilter("\"BookingId\" IS NOT NULL AND \"InvoiceType\" = 'BookingRegistration'");
        builder.HasIndex(i => new { i.RoomId, i.BillingYear, i.BillingMonth, i.CreatedAt })
            .IsDescending(false, true, true, true);
        builder.HasIndex(i => new { i.Status, i.BillingYear, i.BillingMonth });
        builder.HasIndex(i => new { i.BuildingCode, i.Floor, i.BillingYear, i.BillingMonth, i.Status });

        builder.HasOne(i => i.ContractTemplate)
            .WithMany(t => t.Invoices)
            .HasForeignKey(i => i.ContractTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
