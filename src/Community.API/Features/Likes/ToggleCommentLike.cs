using Community.API.Domain.Entities;
using Community.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features.Likes;

public static class ToggleCommentLike
{
    public record Response(
        Guid CommentId,
        bool IsLiked,
        int LikeCount
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/posts/{postId}/comments/{commentId}/like", async (
                Guid postId,
                Guid commentId,
                HttpContext httpContext,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var result = await handler.ExecuteAsync(postId, commentId, userId, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Likes")
            .WithName("ToggleCommentLike")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid postId, Guid commentId, string userId, CancellationToken ct)
        {
            var comment = await dbContext.PostComments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId && !c.IsHidden, ct);

            if (comment is null)
                throw new ApiException("Bình luận không tồn tại hoặc đã bị ẩn.", StatusCodes.Status404NotFound);

            var existingLike = await dbContext.CommentLikes
                .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId, ct);

            bool isLiked;

            if (existingLike is not null)
            {
                // Đã like → unlike
                dbContext.CommentLikes.Remove(existingLike);
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                isLiked = false;
            }
            else
            {
                // Chưa like → like
                dbContext.CommentLikes.Add(new CommentLike
                {
                    CommentId = commentId,
                    UserId = userId,
                });
                comment.LikeCount++;
                isLiked = true;
            }

            await dbContext.SaveChangesAsync(ct);

            return new Response(commentId, isLiked, comment.LikeCount);
        }
    }
}