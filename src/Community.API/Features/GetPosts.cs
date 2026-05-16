using Community.API.Domain.Enums;
using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

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
        string Content,
        List<string> MediaUrls,
        string PostType,
        bool IsPinned,
        bool IsHidden,
        int LikeCount,
        int CommentCount,
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
            app.MapGet("api/posts", async (
                [AsParameters] Query query,
                Handler handler,
                CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(query, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("GetPosts")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Query request, CancellationToken ct)
        {
            if (request.PageSize <= 0 || request.PageSize > 50)
                throw new ApiException("PageSize phải từ 1 đến 50.", StatusCodes.Status400BadRequest);

            if (!string.IsNullOrEmpty(request.PostType) &&
                !Enum.TryParse<PostType>(request.PostType, ignoreCase: true, out _))
                throw new ApiException(
                    $"PostType không hợp lệ. Giá trị hợp lệ: {string.Join(", ", Enum.GetNames<PostType>())}",
                    StatusCodes.Status400BadRequest);

            var pinnedPosts = new List<PostDto>();
            if (string.IsNullOrEmpty(request.Cursor))
            {
                pinnedPosts = await dbContext.Posts
                    .AsNoTracking()
                    .Where(p => p.IsPinned && !p.IsHidden)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PostDto(
                        p.Id, p.AuthorId, p.Content, p.MediaUrls,
                        p.PostType.ToString(), p.IsPinned, p.IsHidden,
                        p.LikeCount, p.CommentCount,
                        p.CreatedAt))
                    .ToListAsync(ct);
            }

            var query = dbContext.Posts
                .AsNoTracking()
                .Where(p => !p.IsHidden && !p.IsPinned)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.PostType) &&
                Enum.TryParse<PostType>(request.PostType, ignoreCase: true, out var parsedType))
            {
                query = query.Where(p => p.PostType == parsedType);
            }

            if (!string.IsNullOrEmpty(request.Cursor) &&
                DateTime.TryParse(request.Cursor, out var cursorTime))
            {
                query = query.Where(p => p.CreatedAt < cursorTime);
            }

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(request.PageSize + 1)
                .Select(p => new PostDto(
                    p.Id, p.AuthorId, p.Content, p.MediaUrls,
                    p.PostType.ToString(), p.IsPinned, p.IsHidden,
                    p.LikeCount, p.CommentCount,
                    p.CreatedAt))
                .ToListAsync(ct);

            var hasMore = items.Count > request.PageSize;
            if (hasMore) items.RemoveAt(items.Count - 1);

            var nextCursor = hasMore
                ? items.Last().CreatedAt.ToString("o")
                : null;

            return new Response(pinnedPosts, items, nextCursor, hasMore);
        }
    }
}