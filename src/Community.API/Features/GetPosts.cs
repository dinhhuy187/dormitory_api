using Community.API.Domain.Enums;
using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Services;
using System.Security.Claims;

namespace Community.API.Features;

public static class GetPosts
{
    public record Query(
        string? Cursor,
        string? PostType,
        int PageSize = 20
    );

    public record PostDto(
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
        DateTime CreatedAt
    );

    public record Response(
        List<PostDto> Pinned,
        List<PostDto> Items,
        string? NextCursor,
        bool HasMore
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/community/posts", async (
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
                    query,
                    userId,
                    accessToken,
                    ct);

                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("GetPosts")
            .RequireAuthorization()
            .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public class Handler(
        CommunityDbContext dbContext,
        IProfileService profileService)
    {
        public async Task<Response> ExecuteAsync(
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

            if (!string.IsNullOrEmpty(request.PostType) &&
                !Enum.TryParse<PostType>(
                    request.PostType,
                    ignoreCase: true,
                    out _))
            {
                throw new ApiException(
                    $"PostType không hợp lệ. Giá trị hợp lệ: {string.Join(", ", Enum.GetNames<PostType>())}",
                    StatusCodes.Status400BadRequest);
            }

            var pinnedRaw = new List<dynamic>();

            if (string.IsNullOrEmpty(request.Cursor))
            {
                pinnedRaw = await dbContext.Posts
                    .AsNoTracking()
                    .Where(p =>
                        p.IsPinned &&
                        !p.IsHidden)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.Id,
                        p.AuthorId,
                        p.Content,
                        p.MediaUrls,
                        p.PostType,
                        p.IsPinned,
                        p.IsHidden,
                        p.LikeCount,
                        p.CommentCount,
                        p.CreatedAt
                    })
                    .ToListAsync<dynamic>(ct);
            }

            var query = dbContext.Posts
                .AsNoTracking()
                .Where(p =>
                    !p.IsHidden &&
                    !p.IsPinned)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.PostType) &&
                Enum.TryParse<PostType>(
                    request.PostType,
                    ignoreCase: true,
                    out var parsedType))
            {
                query = query.Where(p => p.PostType == parsedType);
            }

            if (!string.IsNullOrEmpty(request.Cursor) &&
                DateTime.TryParse(request.Cursor, out var cursorTime))
            {
                query = query.Where(p => p.CreatedAt < cursorTime);
            }

            var itemsRaw = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(request.PageSize + 1)
                .Select(p => new
                {
                    p.Id,
                    p.AuthorId,
                    p.Content,
                    p.MediaUrls,
                    p.PostType,
                    p.IsPinned,
                    p.IsHidden,
                    p.LikeCount,
                    p.CommentCount,
                    p.CreatedAt
                })
                .ToListAsync(ct);

            var hasMore = itemsRaw.Count > request.PageSize;

            if (hasMore)
            {
                itemsRaw.RemoveAt(itemsRaw.Count - 1);
            }

            var nextCursor = hasMore
                ? itemsRaw.Last().CreatedAt.ToString("o")
                : null;


            var authorIds = pinnedRaw
                .Select(x => (string)x.AuthorId)
                .Concat(itemsRaw.Select(x => x.AuthorId))
                .Distinct();

            var profiles = await profileService.GetProfilesAsync(
                authorIds,
                accessToken,
                ct);


            var allPostIds = pinnedRaw
                .Select(x => (Guid)x.Id)
                .Concat(itemsRaw.Select(x => x.Id))
                .Distinct()
                .ToList();

            var likedPostIds = await dbContext.PostLikes
                .AsNoTracking()
                .Where(l =>
                    l.UserId == userId &&
                    allPostIds.Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync(ct);

            var likedSet = likedPostIds.ToHashSet();

            var pinnedPosts = pinnedRaw.Select(p =>
            {
                profiles.TryGetValue((string)p.AuthorId, out var profile);

                return new PostDto(
                    p.Id,
                    p.AuthorId,
                    profile?.FullName ?? "Người dùng",
                    profile?.AvatarUrl,
                    p.Content,
                    p.MediaUrls,
                    p.PostType.ToString(),
                    p.IsPinned,
                    p.IsHidden,
                    p.LikeCount,
                    p.CommentCount,
                    likedSet.Contains((Guid)p.Id),
                    p.CreatedAt
                );
            }).ToList();

            var items = itemsRaw.Select(p =>
            {
                profiles.TryGetValue(p.AuthorId, out var profile);

                return new PostDto(
                    p.Id,
                    p.AuthorId,
                    profile?.FullName ?? "Người dùng",
                    profile?.AvatarUrl,
                    p.Content,
                    p.MediaUrls,
                    p.PostType.ToString(),
                    p.IsPinned,
                    p.IsHidden,
                    p.LikeCount,
                    p.CommentCount,
                    likedSet.Contains(p.Id),
                    p.CreatedAt
                );
            }).ToList();

            return new Response(
                pinnedPosts,
                items,
                nextCursor,
                hasMore);
        }
    }
}