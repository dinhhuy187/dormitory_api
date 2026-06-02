using Billing.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.API.Infrastructure.EntityConfigurations;

public class ServicePriceTierConfiguration : IEntityTypeConfiguration<ServicePriceTier>
{
    public void Configure(EntityTypeBuilder<ServicePriceTier> builder)
    {
        builder.ToTable("ServicePriceTiers", table =>
        {
            table.HasCheckConstraint("CK_ServicePriceTiers_RoomCapacity", "\"RoomCapacity\" IS NULL OR \"RoomCapacity\" > 0");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.ServiceType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.TierName).IsRequired().HasMaxLength(50);
        builder.Property(t => t.RoomCapacity);
        builder.Property(t => t.FromUsage).HasPrecision(18, 2);
        builder.Property(t => t.ToUsage).HasPrecision(18, 2);
        builder.Property(t => t.UnitName).IsRequired().HasMaxLength(20);
        builder.Property(t => t.UnitPrice).HasPrecision(18, 2);
        builder.Property(t => t.EffectiveFrom).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => new { t.ServiceType, t.EffectiveFrom, t.FromUsage })
            .HasDatabaseName("IX_ServicePriceTiers_UQ_GlobalCapacity")
            .HasFilter("\"RoomCapacity\" IS NULL")
            .IsUnique();

        builder.HasIndex(t => new { t.ServiceType, t.EffectiveFrom, t.RoomCapacity, t.FromUsage })
            .HasDatabaseName("IX_ServicePriceTiers_UQ_RoomCapacity")
            .HasFilter("\"RoomCapacity\" IS NOT NULL")
            .IsUnique();

        builder.HasIndex(t => new { t.ServiceType, t.IsActive, t.EffectiveFrom, t.RoomCapacity, t.FromUsage });
    }
}
