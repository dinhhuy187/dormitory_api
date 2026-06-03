using BookingService.Application.Abtractions.Services;
using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Repositories;

namespace BookingService.Application.UseCases.Bookings.Queries.GetMyCurrentRoom;

public sealed class GetMyCurrentRoomUseCase(
    IBookingRepository bookingRepository,
    IRoomDetailReader roomDetailReader) : IGetMyCurrentRoomUseCase
{
    public async Task<Result<CurrentRoomResponse?>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Result<CurrentRoomResponse?>.Failure("UserId is required.");
        }

        var booking = await bookingRepository.GetCurrentRoomBookingByUserIdAsync(userId, cancellationToken);
        if (booking is null)
        {
            return Result<CurrentRoomResponse?>.Success(null);
        }

        var room = await roomDetailReader.GetRoomDetailAsync(booking.RoomId, cancellationToken);
        if (room is null)
        {
            return Result<CurrentRoomResponse?>.Failure("Room not found.");
        }

        return Result<CurrentRoomResponse?>.Success(new CurrentRoomResponse(
            room.Id,
            room.Name,
            room.BuildingName));
    }
}
