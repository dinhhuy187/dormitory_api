using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomService.API.Domain.Entities;

namespace RoomService.API.Infrastructure.EntityConfigurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.ToTable("Rooms");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.RoomNumber).IsRequired().HasMaxLength(20);

            builder.HasIndex(r => new { r.BuildingId, r.RoomNumber }).IsUnique();

            builder.Property(r => r.Floor).IsRequired();

            builder.Property(r => r.RoomStatus).IsRequired();

            builder.Property(r => r.OccupiedCount).IsRequired().HasDefaultValue(0);

            // Relationships
            builder.HasOne(r => r.Building)
                   .WithMany(b => b.Rooms)
                   .HasForeignKey(r => r.BuildingId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.RoomType)
                   .WithMany(rt => rt.Rooms)
                   .HasForeignKey(r => r.RoomTypeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}