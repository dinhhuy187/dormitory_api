using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomService.API.Domain.Entities;

namespace RoomService.API.Infrastructure.EntityConfigurations
{
    public class BuildingConfiguration : IEntityTypeConfiguration<Building>
    {
        public void Configure(EntityTypeBuilder<Building> builder)
        {
            builder.ToTable("Buildings");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.ZoneName).IsRequired().HasMaxLength(50);

            builder.Property(b => b.Code).IsRequired().HasMaxLength(20);
            builder.HasIndex(b => b.Code).IsUnique();

            builder.Property(b => b.Name).IsRequired().HasMaxLength(100);
            builder.Property(b => b.GenderRestriction).IsRequired();
            builder.Property(b => b.TotalFloors).IsRequired();
            builder.Property(b => b.IsActive).IsRequired();
        }
    }
}