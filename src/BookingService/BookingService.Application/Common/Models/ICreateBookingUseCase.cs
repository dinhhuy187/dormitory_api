using BookingService.Application.UseCases.Bookings.Commands.CreateBooking;

namespace BookingService.Application.Common.Models;

public interface ICreateBookingUseCase
{
    Task<Result<CreateBookingResponse>> ExecuteAsync(CreateBookingRequest request, CancellationToken cancellationToken);    
}