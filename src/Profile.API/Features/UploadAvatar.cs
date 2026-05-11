using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Profile.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Profile.API.Features.Profile;

public static class UploadAvatar
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // IFormFile dùng với Minimal API cần DisableAntiforgery
            app.MapPut("/api/profile/me/avatar", async (
                HttpContext httpContext,
                IFormFile file,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                // Validate file — logic giữ nguyên từ controller cũ
                if (file == null || file.Length == 0)
                    return Results.BadRequest(new { message = "Vui lòng chọn một file ảnh." });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return Results.BadRequest(new { message = "Định dạng file không hỗ trợ." });

                if (file.Length > 5 * 1024 * 1024)
                    return Results.BadRequest(new { message = "Dung lượng ảnh quá lớn (Tối đa 5MB)." });

                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? httpContext.User.FindFirstValue("sub")
                             ?? throw new UnauthorizedAccessException();

                var url = await handler.ExecuteAsync(userId, file, ct);
                return Results.Ok(new ApiResponse<string>(url));
            })
            .WithTags("Profile")
            .WithName("UploadAvatar")
            .RequireAuthorization()
            .DisableAntiforgery() // Bắt buộc khi dùng IFormFile với Minimal API
            .Produces<ApiResponse<string>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(ProfileDbContext dbContext, IMediaService mediaService)
    {
        public async Task<string> ExecuteAsync(string userId, IFormFile file, CancellationToken ct)
        {
            var profile = await dbContext.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct)
                ?? throw new ApiException("User profile not found.", StatusCodes.Status404NotFound);

            var avatarUrl = await mediaService.UploadImageAsync(file, "dormitory_avatars");

            profile.AvatarUrl = avatarUrl;
            var result = await dbContext.SaveChangesAsync(ct);

            if (result <= 0)
                throw new ApiException("Cập nhật avatar thất bại.", StatusCodes.Status400BadRequest);

            return avatarUrl;
        }
    }
}