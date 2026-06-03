namespace BookingService.Application.UseCases.Bookings.Queries.GetMyCurrentRoom;

public sealed record CurrentRoomResponse(
    Guid Id,
    string Name,
    string Building);
