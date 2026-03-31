using Profile.API.DTOs;
namespace Profile.API.Services;

public interface IProfileService
{
    Task<ProfileResponse?> GetMyProfileAsync(string userId);
    Task<ProfileResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<string> UploadAvatarAsync(string userId, IFormFile file);
}