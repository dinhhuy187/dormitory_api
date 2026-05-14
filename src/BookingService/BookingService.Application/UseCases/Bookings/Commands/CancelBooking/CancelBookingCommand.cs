namespace BookingService.Application.UseCases.Bookings.Commands.CancelBooking;

public record CancelBookingCommand(Guid BookingId);