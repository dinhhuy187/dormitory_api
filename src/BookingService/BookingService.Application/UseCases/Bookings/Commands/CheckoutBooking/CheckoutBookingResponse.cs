namespace BookingService.Application.UseCases.Bookings.Commands.CheckoutBooking;

public sealed record CheckoutBookingResponse(
    Guid BookingId,
    Guid UserId,
    Guid RoomId,
    string Status,
    DateTime UpdatedAt);
