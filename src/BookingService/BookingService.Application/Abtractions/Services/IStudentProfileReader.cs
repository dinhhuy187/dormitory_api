namespace BookingService.Application.Abtractions.Services;

public interface IStudentProfileReader
{
    Task<IReadOnlyDictionary<Guid, StudentProfileSnapshot>> GetStudentProfilesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}

public sealed record StudentProfileSnapshot(
    Guid UserId,
    string? FullName,
    string? StudentCode,
    string? Email,
    string? PhoneNumber,
    string? Gender,
    DateOnly? DateOfBirth,
    string? AvatarUrl,
    string? StudentYear,
    string? School,
    string? Faculty,
    bool ProfileExists);
