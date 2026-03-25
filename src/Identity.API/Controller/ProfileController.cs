using System.Security.Claims;
using Identity.API.Service;
using Identity.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Shared;

namespace Identity.API.Controllers;

[Route("api/profile")]
[ApiController]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _profileService.GetProfileAsync(userId);
            return Ok(new ApiResponse<ProfileResponseDto>(profile));
        }
        catch (ApiException ex)
        {
            // Bắt lỗi nghiệp vụ đã định nghĩa trước (ví dụ: không tìm thấy User)
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Bắt các lỗi hệ thống không mong muốn
            return StatusCode(500, new { message = "An internal error occurred.", details = ex.Message });
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _profileService.UpdateProfileAsync(userId, request);
            return Ok(new ApiResponse<ProfileResponseDto>(profile));
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating profile.", details = ex.Message });
        }
    }

    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Kiểm tra file sơ bộ trước khi gửi vào Service
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var avatarUrl = await _profileService.UploadAvatarAsync(userId, file);
            var data = new { avatarUrl };

            return Ok(new ApiResponse<object>(data));
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error uploading avatar.", details = ex.Message });
        }
    }
}