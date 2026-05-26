using Chat.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Messages;

public static class GetMessages
{
    public record Query(string? Cursor, int PageSize = 30);

    public record MessageDto(
        Guid Id,
        string SenderId,
        string Content,
        List<string> MediaUrls,
        bool IsDeleted,
        bool IsMe,
        int ReadCount,
        DateTime CreatedAt
    );

    public record Response(
        List<MessageDto> Items,
        string? NextCursor,
        bool HasMore
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/conversations/{conversationId}/messages", async (
                Guid conversationId,
                [AsParameters] Query query,
                HttpContext httpContext,
                Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var result = await handler.ExecuteAsync(conversationId, userId, query, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Messages")
            .WithName("GetMessages")
            .RequireAuthorization()
            .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public class Handler(ChatDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid conversationId, string userId, Query request, CancellationToken ct)
        {
            if (request.PageSize <= 0 || request.PageSize > 100)
                throw new ApiException("PageSize phải từ 1 đến 100.", StatusCodes.Status400BadRequest);

            var isMember = await dbContext.ConversationMembers
                .AnyAsync(m => m.ConversationId == conversationId
                            && m.UserId == userId
                            && !m.IsDeleted, ct);

            if (!isMember)
                throw new ApiException("Bạn không thuộc cuộc trò chuyện này.", StatusCodes.Status403Forbidden);

            var query = dbContext.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .AsQueryable();

            // Cursor: lấy tin nhắn cũ hơn cursor (scroll lên trên)
            if (!string.IsNullOrEmpty(request.Cursor) &&
                DateTime.TryParse(request.Cursor, out var cursorTime))
            {
                query = query.Where(m => m.CreatedAt < cursorTime);
            }

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(request.PageSize + 1)
                .Select(m => new MessageDto(
                    m.Id,
                    m.SenderId,
                    m.IsDeleted ? "Tin nhắn đã bị xóa." : m.Content,
                    m.IsDeleted ? new List<string>() : m.MediaUrls,
                    m.IsDeleted,
                    m.SenderId == userId,
                    m.ReadReceipts.Count,
                    m.CreatedAt
                ))
                .ToListAsync(ct);

            var hasMore = items.Count > request.PageSize;
            if (hasMore) items.RemoveAt(items.Count - 1);

            // Trả về theo thứ tự cũ → mới để client render đúng
            items.Reverse();

            var nextCursor = hasMore
                ? items.First().CreatedAt.ToString("o")
                : null;

            return new Response(items, nextCursor, hasMore);
        }
    }
}