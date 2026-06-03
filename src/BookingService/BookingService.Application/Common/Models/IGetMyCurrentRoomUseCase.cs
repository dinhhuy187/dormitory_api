using BookingService.Application.Common;
using BookingService.Application.UseCases.Bookings.Queries.GetMyCurrentRoom;

namespace BookingService.Application.Common.Models;

public interface IGetMyCurrentRoomUseCase
{
    Task<Result<CurrentRoomResponse?>> ExecuteAsync(Guid userId, CancellationToken cancellationToken);
}
