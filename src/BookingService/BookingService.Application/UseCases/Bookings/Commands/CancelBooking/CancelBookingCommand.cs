namespace BookingService.Application.UseCases.Bookings.Commands.CancelBooking;

public record CancelBookingCommand(Guid BookingId)
{
    public CancelBookingCommand() : this(Guid.Empty) { }
}