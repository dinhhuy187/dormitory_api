using BookingService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configurations;

public class RoomDataConfiguration : IEntityTypeConfiguration<RoomData>
{
    public void Configure(EntityTypeBuilder<RoomData> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MonthlyPrice).HasColumnType("numeric(18,2)");
        builder.Property(x => x.RoomName).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Capacity).IsRequired();
        builder.Property(x => x.OccupiedCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("AVAILABLE");
    }
}