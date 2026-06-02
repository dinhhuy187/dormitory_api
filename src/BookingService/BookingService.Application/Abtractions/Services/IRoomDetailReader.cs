namespace BookingService.Application.Abtractions.Services;

public interface IRoomDetailReader
{
    Task<RoomDetailSnapshot?> GetRoomDetailAsync(Guid roomId, CancellationToken cancellationToken);
}

public sealed record RoomDetailSnapshot(
    Guid Id,
    string Name,
    Guid BuildingId,
    string BuildingName,
    int Floor,
    int Capacity,
    int OccupiedCount,
    decimal OccupancyPercent,
    string Description,
    string RoomStatus,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal BasePrice,
    IReadOnlyList<string> Amenities);
