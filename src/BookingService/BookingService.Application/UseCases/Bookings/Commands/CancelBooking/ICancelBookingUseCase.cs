using BookingService.Application.Common;

namespace BookingService.Application.UseCases.Bookings.Commands.CancelBooking;

public interface ICancelBookingUseCase
{
    Task<Result<bool>> ExecuteAsync(CancelBookingCommand request, CancellationToken cancellationToken = default);
}