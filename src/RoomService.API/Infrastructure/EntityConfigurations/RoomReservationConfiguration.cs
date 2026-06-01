using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomService.API.Domain.Entities;

namespace RoomService.API.Infrastructure.EntityConfigurations;

public class RoomReservationConfiguration : IEntityTypeConfiguration<RoomReservation>
{
    public void Configure(EntityTypeBuilder<RoomReservation> builder)
    {
        builder.ToTable("RoomReservations");

        builder.HasKey(reservation => reservation.Id);
        builder.HasIndex(reservation => reservation.BookingId).IsUnique();
        builder.HasIndex(reservation => reservation.RoomId);
        builder.HasIndex(reservation => new { reservation.RoomId, reservation.Status });

        builder.Property(reservation => reservation.Status).IsRequired();
        builder.Property(reservation => reservation.ReservedAt).IsRequired();
        builder.Property(reservation => reservation.ReleaseReason).HasMaxLength(255);

        builder.HasOne(reservation => reservation.Room)
            .WithMany()
            .HasForeignKey(reservation => reservation.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
