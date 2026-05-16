using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

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
        string Content,
        int LikeCount,
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
                Handler handler,
                CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(postId, query, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Comments")
            .WithName("GetComments")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid postId, Query request, CancellationToken ct)
        {
            if (request.PageSize <= 0 || request.PageSize > 50)
                throw new ApiException("PageSize phải từ 1 đến 50.", StatusCodes.Status400BadRequest);

            var postExists = await dbContext.Posts
                .AnyAsync(p => p.Id == postId && !p.IsHidden, ct);

            if (!postExists)
                throw new ApiException("Bài viết không tồn tại hoặc đã bị ẩn.", StatusCodes.Status404NotFound);

            var query = dbContext.PostComments
                .AsNoTracking()
                .Where(c => c.PostId == postId && !c.IsHidden)
                .AsQueryable();

            // Cursor-based pagination — cũ hơn cursor
            if (!string.IsNullOrEmpty(request.Cursor) &&
                DateTime.TryParse(request.Cursor, out var cursorTime))
            {
                query = query.Where(c => c.CreatedAt > cursorTime);
            }

            var items = await query
                .OrderBy(c => c.CreatedAt)
                .Take(request.PageSize + 1)
                .Select(c => new CommentDto(
                    c.Id,
                    c.PostId,
                    c.AuthorId,
                    c.Content,
                    c.LikeCount,
                    c.IsHidden,
                    c.CreatedAt
                ))
                .ToListAsync(ct);

            var hasMore = items.Count > request.PageSize;
            if (hasMore) items.RemoveAt(items.Count - 1);

            var nextCursor = hasMore
                ? items.Last().CreatedAt.ToString("o")
                : null;

            return new Response(items, nextCursor, hasMore);
        }
    }
}