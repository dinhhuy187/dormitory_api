namespace BookingService.Application.UseCases.Bookings.Commands.CreateBooking;

public record CreateBookingRequest(Guid RoomId, Guid UserId, string TermName, List<string>? SelectedOptionalFeeCodes = null);