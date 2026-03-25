using Identity.API.DTOs;
using Microsoft.AspNetCore.Http;

namespace Identity.API.Service;

public interface IProfileService
{
    Task<ProfileResponseDto> GetProfileAsync(string id);
    Task<ProfileResponseDto> UpdateProfileAsync(string id, UpdateProfileRequestDto profile);
    Task<string> UploadAvatarAsync(string userId, IFormFile file);
}