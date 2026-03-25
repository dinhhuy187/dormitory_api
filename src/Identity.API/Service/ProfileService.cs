using Identity.API.DTOs;
using Identity.API.Data;
using Microsoft.AspNetCore.Identity;
using Shared;

namespace Identity.API.Service;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediaService _mediaService;

    public ProfileService(UserManager<ApplicationUser> userManager, IMediaService mediaService)
    {
        _userManager = userManager;
        _mediaService = mediaService;
    }

    public async Task<ProfileResponseDto> GetProfileAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            throw new ApiException("User not found.", StatusCodes.Status404NotFound);
        }
        return MapToDto(user);
    }

    public async Task<ProfileResponseDto> UpdateProfileAsync(string id, UpdateProfileRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            throw new ApiException("User not found.", StatusCodes.Status404NotFound);
        }
        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        user.Bio = request.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApiException("Cập nhật thất bại", StatusCodes.Status400BadRequest);
        }
        return MapToDto(user);

    }

    public async Task<string> UploadAvatarAsync(string userId, IFormFile file)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException("User not found.", StatusCodes.Status404NotFound);
        }
        var avatarUrl = await _mediaService.UploadImageAsync(file, "dormitory_avatars");
        user.AvatarUrl = avatarUrl;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApiException("Cập nhật thất bại", StatusCodes.Status400BadRequest);
        }
        return avatarUrl;
    }

    private ProfileResponseDto MapToDto(ApplicationUser user)
    {
        return new ProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            StudentCode = user.StudentCode
        };
    }
}