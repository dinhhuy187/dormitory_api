using RoomService.API.Domain.Enum;

namespace RoomService.API.Domain.Entities
{
    public class Room
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BuildingId { get; set; }
        public Guid RoomTypeId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public int OccupiedCount { get; set; } = 0;
        public RoomStatus RoomStatus { get; set; } = RoomStatus.Available;

        // Navigation Properties
        public virtual Building? Building { get; set; }
        public virtual RoomType? RoomType { get; set; }
    }
}