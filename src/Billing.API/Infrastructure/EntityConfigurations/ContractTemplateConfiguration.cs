using Billing.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.API.Infrastructure.EntityConfigurations;

public class ContractTemplateConfiguration : IEntityTypeConfiguration<ContractTemplate>
{
    public void Configure(EntityTypeBuilder<ContractTemplate> builder)
    {
        builder.ToTable("ContractTemplates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Content).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.EffectiveFrom).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => new { t.Code, t.Version }).IsUnique();
        builder.HasIndex(t => new { t.IsActive, t.EffectiveFrom });
    }
}
