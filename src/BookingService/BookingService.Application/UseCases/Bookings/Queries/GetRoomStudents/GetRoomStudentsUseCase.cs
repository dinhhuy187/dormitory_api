using BookingService.Application.Abtractions.Services;
using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Repositories;

namespace BookingService.Application.UseCases.Bookings.Queries.GetRoomStudents;

public sealed class GetRoomStudentsUseCase(
    IBookingRepository bookingRepository,
    IStudentProfileReader studentProfileReader,
    IRoomDetailReader roomDetailReader) : IGetRoomStudentsUseCase
{
    public async Task<Result<RoomStudentsResponse>> ExecuteAsync(
        GetRoomStudentsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result<RoomStudentsResponse>.Failure("UserId is required.");
        }

        var currentBooking = await bookingRepository.GetCurrentRoomBookingByUserIdAsync(query.UserId, cancellationToken);
        if (currentBooking is null)
        {
            return Result<RoomStudentsResponse>.Failure("No active or confirmed booking found for current user.");
        }

        var roomId = currentBooking.RoomId;
        var room = await roomDetailReader.GetRoomDetailAsync(roomId, cancellationToken);
        if (room is null)
        {
            return Result<RoomStudentsResponse>.Failure("Room not found.");
        }

        var roomResponse = MapRoom(room);
        var bookings = await bookingRepository.GetRoomOccupantBookingsAsync(roomId, cancellationToken);
        if (bookings.Count == 0)
        {
            return Result<RoomStudentsResponse>.Success(new RoomStudentsResponse(
                roomId,
                roomResponse,
                0,
                []));
        }

        var userIds = bookings
            .Select(booking => booking.UserId)
            .Distinct()
            .ToList();

        var profiles = await studentProfileReader.GetStudentProfilesAsync(userIds, cancellationToken);

        var students = bookings
            .Select(booking =>
            {
                profiles.TryGetValue(booking.UserId, out var profile);

                return new RoomStudentResponse(
                    StudentId: booking.UserId,
                    BookingId: booking.Id,
                    Status: booking.Status.ToString(),
                    TermName: booking.Term.TermName,
                    StartDate: booking.Term.StartDate,
                    EndDate: booking.Term.EndDate,
                    FullName: profile?.FullName,
                    StudentCode: profile?.StudentCode,
                    Email: profile?.Email,
                    PhoneNumber: profile?.PhoneNumber,
                    Gender: profile?.Gender,
                    DateOfBirth: profile?.DateOfBirth?.ToString("yyyy-MM-dd"),
                    AvatarUrl: profile?.AvatarUrl,
                    StudentYear: profile?.StudentYear,
                    School: profile?.School,
                    Faculty: profile?.Faculty,
                    ProfileExists: profile?.ProfileExists ?? false);
            })
            .ToList();

        return Result<RoomStudentsResponse>.Success(new RoomStudentsResponse(
            roomId,
            roomResponse,
            students.Count,
            students));
    }

    private static RoomDetailResponse MapRoom(RoomDetailSnapshot room)
    {
        return new RoomDetailResponse(
            room.Id,
            room.Name,
            room.BuildingId,
            room.BuildingName,
            room.Floor,
            room.Capacity,
            room.OccupiedCount,
            room.OccupancyPercent,
            room.Description,
            room.RoomStatus,
            room.RoomTypeId,
            room.RoomTypeName,
            room.BasePrice,
            room.Amenities);
    }
}
