using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Community.API.Features;

public static class HidePost
{
    public record Response(Guid Id, bool IsHidden, DateTime UpdatedAt);

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/community/posts/{id}/hide", async (
                Guid id,
                HttpContext httpContext,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var isAdminOrStaff = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Staff");

                var result = await handler.ExecuteAsync(id, userId, isAdminOrStaff, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("HidePost")
            .RequireAuthorization()
            .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid id, string userId, bool isAdminOrStaff, CancellationToken ct)
        {
            var post = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (post is null)
                throw new ApiException("Bài viết không tồn tại.", StatusCodes.Status404NotFound);

            // Chỉ tác giả hoặc Admin/Staff mới được ẩn/hiện bài
            if (post.AuthorId != userId && !isAdminOrStaff)
                throw new ApiException("Bạn không có quyền thực hiện hành động này.", StatusCodes.Status403Forbidden);

            // Toggle trạng thái ẩn
            post.IsHidden = !post.IsHidden;

            // Nếu đang ẩn thì bỏ ghim
            if (post.IsHidden)
            {
                post.IsPinned = false;
            }

            post.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return new Response(post.Id, post.IsHidden, post.UpdatedAt);
        }
    }
}