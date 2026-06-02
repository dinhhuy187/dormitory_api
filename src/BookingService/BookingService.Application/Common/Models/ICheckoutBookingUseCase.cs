using BookingService.Application.Common;
using BookingService.Application.UseCases.Bookings.Commands.CheckoutBooking;

namespace BookingService.Application.Common.Models;

public interface ICheckoutBookingUseCase
{
    Task<Result<CheckoutBookingResponse>> ExecuteAsync(Guid userId, CancellationToken cancellationToken);
}
