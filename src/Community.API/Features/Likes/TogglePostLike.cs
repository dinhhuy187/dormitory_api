using Community.API.Domain.Entities;
using Community.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features.Likes;

public static class TogglePostLike
{
    public record Response(
        Guid PostId,
        bool IsLiked,
        int LikeCount
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/posts/{postId}/like", async (
                Guid postId,
                HttpContext httpContext,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var result = await handler.ExecuteAsync(postId, userId, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Likes")
            .WithName("TogglePostLike")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid postId, string userId, CancellationToken ct)
        {
            var post = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsHidden, ct);

            if (post is null)
                throw new ApiException("Bài viết không tồn tại hoặc đã bị ẩn.", StatusCodes.Status404NotFound);

            var existingLike = await dbContext.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, ct);

            bool isLiked;

            if (existingLike is not null)
            {
                // Đã like → unlike
                dbContext.PostLikes.Remove(existingLike);
                post.LikeCount = Math.Max(0, post.LikeCount - 1);
                isLiked = false;
            }
            else
            {
                // Chưa like → like
                dbContext.PostLikes.Add(new PostLike
                {
                    PostId = postId,
                    UserId = userId,
                });
                post.LikeCount++;
                isLiked = true;
            }

            await dbContext.SaveChangesAsync(ct);

            return new Response(postId, isLiked, post.LikeCount);
        }
    }
}