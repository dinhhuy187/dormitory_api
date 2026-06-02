using BookingService.Application.Common;
using BookingService.Application.UseCases.Bookings.Queries.GetRoomStudents;

namespace BookingService.Application.Common.Models;

public interface IGetRoomStudentsUseCase
{
    Task<Result<RoomStudentsResponse>> ExecuteAsync(
        GetRoomStudentsQuery query,
        CancellationToken cancellationToken);

    Task<Result<RoomStudentsResponse>> ExecuteByRoomIdAsync(
        GetRoomStudentsByRoomQuery query,
        CancellationToken cancellationToken);
}
