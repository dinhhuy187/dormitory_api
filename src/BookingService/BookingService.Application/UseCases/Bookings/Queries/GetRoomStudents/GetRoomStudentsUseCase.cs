using BookingService.Application.Abtractions.Services;
using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Repositories;

namespace BookingService.Application.UseCases.Bookings.Queries.GetRoomStudents;

public sealed class GetRoomStudentsUseCase(
    IBookingRepository bookingRepository,
    IStudentProfileReader studentProfileReader) : IGetRoomStudentsUseCase
{
    public async Task<Result<RoomStudentsResponse>> ExecuteAsync(
        GetRoomStudentsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.RoomId == Guid.Empty)
        {
            return Result<RoomStudentsResponse>.Failure("RoomId is required.");
        }

        var bookings = await bookingRepository.GetRoomOccupantBookingsAsync(query.RoomId, cancellationToken);
        if (bookings.Count == 0)
        {
            return Result<RoomStudentsResponse>.Success(new RoomStudentsResponse(
                query.RoomId,
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
            query.RoomId,
            students.Count,
            students));
    }
}
