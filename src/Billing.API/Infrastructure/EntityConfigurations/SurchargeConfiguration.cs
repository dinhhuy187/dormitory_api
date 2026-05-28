using Billing.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.API.Infrastructure.EntityConfigurations;

public class SurchargeConfiguration : IEntityTypeConfiguration<Surcharge>
{
    public void Configure(EntityTypeBuilder<Surcharge> builder)
    {
        builder.ToTable("Surcharges", table =>
        {
            table.HasCheckConstraint("CK_Surcharges_Amount", "\"Amount\" > 0");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Amount).HasPrecision(18, 2);
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => s.InvoiceId);

        builder.HasOne(s => s.Invoice)
            .WithMany(i => i.Surcharges)
            .HasForeignKey(s => s.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
