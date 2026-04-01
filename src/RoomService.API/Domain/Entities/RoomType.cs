namespace RoomService.API.Domain.Entities
{
    public class RoomType
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; } 
        public decimal BasePrice { get; set; } 
        public List<string> Amenities { get; set; } = new List<string>();
    
        // Navigation Property
        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}