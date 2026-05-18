namespace Chat.API.Infrastructure.Services;

public interface IProfileService
{
    Task<Dictionary<string, UserProfileDto>> GetProfilesAsync(
        IEnumerable<string> userIds, string accessToken, CancellationToken ct);
}

public record UserProfileDto(
    string UserId,
    string FullName,
    string? AvatarUrl
);