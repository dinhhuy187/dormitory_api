using RoomService.API.Domain.Enum;

namespace RoomService.API.Domain.Entities
{
    public class Building
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ZoneName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public GenderRestriction GenderRestriction { get; set; }
        public int TotalFloors { get; set; }
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
        public bool IsActive { get; set; } = true;
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
