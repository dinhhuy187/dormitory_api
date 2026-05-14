using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configurations;

public class FeeTemplateConfiguration : IEntityTypeConfiguration<FeeTemplate>
{
    public void Configure(EntityTypeBuilder<FeeTemplate> builder)
    {
        builder.ToTable("FeeTemplates");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FeeCode).HasMaxLength(20).IsRequired();
        builder.Property(f => f.FeeName).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Amount).HasColumnType("numeric(18,2)");
        builder.Property(f => f.Description).HasMaxLength(255);
        builder.Property(f => f.IsMandatory).IsRequired();
        builder.Property(f => f.IsRefundable).IsRequired();
    }
}