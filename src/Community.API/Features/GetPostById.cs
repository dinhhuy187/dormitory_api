using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Services;
using System.Security.Claims;

namespace Community.API.Features;

public static class GetPostById
{
    public record Response(
        Guid Id,
        string AuthorId,
        string AuthorName,
        string? AvatarUrl,
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

                var accessToken = httpContext.Request.Headers.Authorization
                    .ToString()
                    .Replace("Bearer ", string.Empty);

                var result = await handler.ExecuteAsync(
                    postId,
                    userId,
                    accessToken,
                    ct);

                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("GetPostById")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(
        CommunityDbContext dbContext,
        IProfileService profileService)
    {
        public async Task<Response> ExecuteAsync(
            Guid postId,
            string userId,
            string accessToken,
            CancellationToken ct)
        {
            var post = await dbContext.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post is null)
                throw new ApiException(
                    "Bài viết không tồn tại.",
                    StatusCodes.Status404NotFound);

            if (post.IsHidden)
                throw new ApiException(
                    "Bài viết đã bị ẩn.",
                    StatusCodes.Status404NotFound);

            var isLikedByMe = await dbContext.PostLikes
                .AnyAsync(
                    l => l.PostId == postId && l.UserId == userId,
                    ct);

            // Lấy profile tác giả
            var profiles = await profileService.GetProfilesAsync(
                new[] { post.AuthorId },
                accessToken,
                ct);

            profiles.TryGetValue(post.AuthorId, out var profile);

            return new Response(
                post.Id,
                post.AuthorId,
                profile?.FullName ?? "Người dùng",
                profile?.AvatarUrl,
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