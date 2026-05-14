namespace BookingService.Application.UseCases.Bookings.Commands.CreateBooking;

public record CreateBookingResponse(Guid BookingId, string Status, decimal TotalPrice, string Message);