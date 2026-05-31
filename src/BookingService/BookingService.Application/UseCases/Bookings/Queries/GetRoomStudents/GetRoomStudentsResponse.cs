namespace BookingService.Application.UseCases.Bookings.Queries.GetRoomStudents;

public sealed record GetRoomStudentsQuery(Guid RoomId);

public sealed record RoomStudentsResponse(
    Guid RoomId,
    int TotalStudents,
    IReadOnlyList<RoomStudentResponse> Students);

public sealed record RoomStudentResponse(
    Guid StudentId,
    Guid BookingId,
    string Status,
    string TermName,
    DateTime StartDate,
    DateTime EndDate,
    string? FullName,
    string? StudentCode,
    string? Email,
    string? PhoneNumber,
    string? Gender,
    string? DateOfBirth,
    string? AvatarUrl,
    string? StudentYear,
    string? School,
    string? Faculty,
    bool ProfileExists);
