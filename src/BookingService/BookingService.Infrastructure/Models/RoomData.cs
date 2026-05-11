namespace BookingService.Infrastructure.Models;

public class RoomData
{
    public Guid Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int Capacity { get; set; }
    public int OccupiedCount { get; set; }
    public string Status { get; set; } = "AVAILABLE";
}