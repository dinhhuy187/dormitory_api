using Community.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features.Comments;

public static class DeleteComment
{
    public record Response(Guid Id, bool IsHidden, DateTime UpdatedAt);

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/posts/{postId}/comments/{commentId}", async (
                Guid postId,
                Guid commentId,
                HttpContext httpContext,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var isAdminOrStaff = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Staff");

                var result = await handler.ExecuteAsync(postId, commentId, userId, isAdminOrStaff, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Comments")
            .WithName("DeleteComment")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid postId, Guid commentId, string userId, bool isAdminOrStaff, CancellationToken ct)
        {
            var comment = await dbContext.PostComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId, ct);

            if (comment is null)
                throw new ApiException("Bình luận không tồn tại.", StatusCodes.Status404NotFound);

            // Chủ comment hoặc Admin/Staff mới được xóa
            if (comment.AuthorId != userId && !isAdminOrStaff)
                throw new ApiException("Bạn không có quyền xóa bình luận này.", StatusCodes.Status403Forbidden);

            // Soft delete — ẩn comment
            comment.IsHidden = true;
            comment.UpdatedAt = DateTime.UtcNow;

            // Giảm counter trên Post
            if (comment.Post is not null && comment.Post.CommentCount > 0)
                comment.Post.CommentCount--;

            await dbContext.SaveChangesAsync(ct);

            return new Response(comment.Id, comment.IsHidden, comment.UpdatedAt);
        }
    }
}