using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Services;
using System.Security.Claims;

namespace Community.API.Features.Comments;

public static class GetComments
{
    public record Query(
        string? Cursor,
        int PageSize = 20
    );

    public record CommentDto(
        Guid Id,
        Guid PostId,
        string AuthorId,
        string AuthorName,
        string? AvatarUrl,
        string Content,
        int LikeCount,
        bool IsLikedByMe,
        bool IsHidden,
        DateTime CreatedAt
    );

    public record Response(
        List<CommentDto> Items,
        string? NextCursor,
        bool HasMore
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/posts/{postId}/comments", async (
                Guid postId,
                [AsParameters] Query query,
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
                    query,
                    userId,
                    accessToken,
                    ct);

                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Comments")
            .WithName("GetComments")
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
            Query request,
            string userId,
            string accessToken,
            CancellationToken ct)
        {
            if (request.PageSize <= 0 || request.PageSize > 50)
            {
                throw new ApiException(
                    "PageSize phải từ 1 đến 50.",
                    StatusCodes.Status400BadRequest);
            }

            var postExists = await dbContext.Posts
                .AnyAsync(
                    p => p.Id == postId && !p.IsHidden,
                    ct);

            if (!postExists)
            {
                throw new ApiException(
                    "Bài viết không tồn tại hoặc đã bị ẩn.",
                    StatusCodes.Status404NotFound);
            }

            var query = dbContext.PostComments
                .AsNoTracking()
                .Where(c =>
                    c.PostId == postId &&
                    !c.IsHidden)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Cursor) &&
                DateTime.TryParse(request.Cursor, out var cursorTime))
            {
                query = query.Where(c => c.CreatedAt > cursorTime);
            }

            var items = await query
                .OrderBy(c => c.CreatedAt)
                .Take(request.PageSize + 1)
                .Select(c => new
                {
                    c.Id,
                    c.PostId,
                    c.AuthorId,
                    c.Content,
                    c.LikeCount,
                    c.IsHidden,
                    c.CreatedAt
                })
                .ToListAsync(ct);

            var hasMore = items.Count > request.PageSize;

            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            var nextCursor = hasMore
                ? items.Last().CreatedAt.ToString("o")
                : null;

            var authorIds = items
                .Select(c => c.AuthorId)
                .Distinct();

            var profiles = await profileService.GetProfilesAsync(
                authorIds,
                accessToken,
                ct);

            var commentIds = items
                .Select(c => c.Id)
                .ToList();

            var likedCommentIds = await dbContext.CommentLikes
                .AsNoTracking()
                .Where(l =>
                    l.UserId == userId &&
                    commentIds.Contains(l.CommentId))
                .Select(l => l.CommentId)
                .ToListAsync(ct);

            var likedSet = likedCommentIds.ToHashSet();

            var dtos = items.Select(c =>
            {
                profiles.TryGetValue(c.AuthorId, out var profile);

                return new CommentDto(
                    c.Id,
                    c.PostId,
                    c.AuthorId,
                    profile?.FullName ?? "Người dùng",
                    profile?.AvatarUrl,
                    c.Content,
                    c.LikeCount,
                    likedSet.Contains(c.Id),
                    c.IsHidden,
                    c.CreatedAt
                );
            }).ToList();

            return new Response(
                dtos,
                nextCursor,
                hasMore);
        }
    }
}