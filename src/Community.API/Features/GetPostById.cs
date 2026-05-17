using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features;

public static class GetPostById
{
    public record Response(
        Guid Id,
        string AuthorId,
        string Content,
        List<string> MediaUrls,
        string PostType,
        bool IsPinned,
        bool IsHidden,
        int LikeCount,
        int CommentCount,
        bool IsLikedByMe,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/posts/{postId}", async (
                Guid postId,
                HttpContext httpContext,
                Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var result = await handler.ExecuteAsync(postId, userId, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("GetPostById")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid postId, string userId, CancellationToken ct)
        {
            var post = await dbContext.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post is null)
                throw new ApiException("Bài viết không tồn tại.", StatusCodes.Status404NotFound);

            if (post.IsHidden)
                throw new ApiException("Bài viết đã bị ẩn.", StatusCodes.Status404NotFound);

            var isLikedByMe = await dbContext.PostLikes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId, ct);

            return new Response(
                post.Id,
                post.AuthorId,
                post.Content,
                post.MediaUrls,
                post.PostType.ToString(),
                post.IsPinned,
                post.IsHidden,
                post.LikeCount,
                post.CommentCount,
                isLikedByMe,
                post.CreatedAt,
                post.UpdatedAt
            );
        }
    }
}