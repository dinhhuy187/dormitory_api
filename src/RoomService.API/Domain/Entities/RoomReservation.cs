using RoomService.API.Domain.Enum;

namespace RoomService.API.Domain.Entities;

public class RoomReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid RoomId { get; set; }
    public Guid StudentId { get; set; }
    public RoomReservationStatus Status { get; set; } = RoomReservationStatus.Reserved;
    public DateTime ReservedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }

    public Room? Room { get; set; }
}
