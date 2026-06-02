namespace BookingService.Application.UseCases.Bookings.Commands.ConfirmBooking;

public record ConfirmBookingCommand(Guid BookingId)
{
    public ConfirmBookingCommand() : this(Guid.Empty) { }
}