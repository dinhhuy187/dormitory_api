using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.API.DTOs;
using Profile.API.Services;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Shared;

namespace Profile.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileServie;
    public ProfileController(IProfileService profileService)
    {
        _profileServie = profileService;
    }

    private string getUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? throw new UnauthorizedAccessException();
        return userId;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = this.getUserId();
        var data = await this._profileServie.GetMyProfileAsync(userId);
        return Ok(new ApiResponse<object>(data));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = this.getUserId();
        var data = await this._profileServie.UpdateProfileAsync(userId, request);
        return Ok(new ApiResponse<object>(data));
    }

    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn một file ảnh." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Định dạng file không hỗ trợ. Chỉ nhận .jpg, .png, .webp" });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "Dung lượng ảnh quá lớn (Tối đa 5MB)." });
        }

        var userId = this.getUserId();

        var data = await _profileServie.UploadAvatarAsync(userId, file);

        return Ok(new ApiResponse<object>(data));
    }
}