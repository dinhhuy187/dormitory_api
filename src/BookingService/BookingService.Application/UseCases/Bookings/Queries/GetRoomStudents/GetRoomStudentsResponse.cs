namespace BookingService.Application.UseCases.Bookings.Queries.GetRoomStudents;

public sealed record GetRoomStudentsQuery(Guid RoomId);

public sealed record RoomStudentsResponse(
    Guid RoomId,
    RoomDetailResponse Room,
    int TotalStudents,
    IReadOnlyList<RoomStudentResponse> Students);

public sealed record RoomDetailResponse(
    Guid Id,
    string Name,
    Guid BuildingId,
    string BuildingName,
    int Floor,
    int Capacity,
    int OccupiedCount,
    decimal OccupancyPercent,
    string Description,
    string RoomStatus,
    Guid RoomTypeId,
    string RoomTypeName,
    decimal BasePrice,
    IReadOnlyList<string> Amenities);

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
