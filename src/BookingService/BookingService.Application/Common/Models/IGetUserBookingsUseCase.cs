using BookingService.Application.Common;
using BookingService.Application.UseCases.Bookings.Queries.GetUserBookings;

namespace BookingService.Application.Common.Models;

public interface IGetUserBookingsUseCase
{
    Task<Result<List<BookingItemResponse>>> ExecuteAsync(Guid userId, CancellationToken cancellationToken);
}
