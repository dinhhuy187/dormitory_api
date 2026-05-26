using Chat.API.Infrastructure.Database;
using Shared.Services;
using Chat.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Conversations;

public static class GetMyConversations
{
    public record ConversationDto(
        Guid Id,
        string Type,
        string? Name,
        string? AvatarUrl,
        int MemberCount,
        string? LastMessage,
        DateTime? LastMessageAt,
        DateTime CreatedAt
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/conversations", async (
                HttpContext httpContext,
                Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                // Lấy token từ header Authorization
                var accessToken = httpContext.Request.Headers.Authorization
                    .ToString()
                    .Replace("Bearer ", string.Empty);

                var result = await handler.ExecuteAsync(userId, accessToken, ct);
                return Results.Ok(new ApiResponse<List<ConversationDto>>(result));
            })
            .WithTags("Conversations")
            .WithName("GetMyConversations")
            .RequireAuthorization()
            .Produces<List<ConversationDto>>(StatusCodes.Status200OK);
        }
    }
    public class Handler(ChatDbContext dbContext, IProfileService profileService)
    {
        public async Task<List<ConversationDto>> ExecuteAsync(
            string userId,
            string accessToken,
            CancellationToken ct)
        {
            var conversations = await dbContext.Conversations
                .AsNoTracking()
                .Where(c => c.Members.Any(m => m.UserId == userId && !m.IsDeleted))
                .Select(c => new
                {
                    c.Id,
                    c.Type,
                    c.Name,
                    c.AvatarUrl,
                    MemberCount = c.Members.Count(m => !m.IsDeleted),
                    OtherUserId = c.Type == ConversationType.Direct
                        ? c.Members
                            .Where(m => m.UserId != userId && !m.IsDeleted)
                            .Select(m => m.UserId)
                            .FirstOrDefault()
                        : null,
                    LastMessage = c.Messages
                        .Where(m => !m.IsDeleted)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),
                    LastMessageAt = c.Messages
                        .Where(m => !m.IsDeleted)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => (DateTime?)m.CreatedAt)
                        .FirstOrDefault(),
                    c.CreatedAt
                })
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync(ct);

            var otherUserIds = conversations
                .Where(c => c.OtherUserId is not null)
                .Select(c => c.OtherUserId!)
                .Distinct();

            // Truyền token vào đây
            var profiles = await profileService.GetProfilesAsync(otherUserIds, accessToken, ct);

            return conversations.Select(c =>
            {
                if (c.Type == ConversationType.Direct && c.OtherUserId is not null)
                {
                    profiles.TryGetValue(c.OtherUserId, out var profile);
                    return new ConversationDto(
                        c.Id,
                        c.Type.ToString(),
                        profile?.FullName ?? "Người dùng",
                        profile?.AvatarUrl,
                        c.MemberCount,
                        c.LastMessage,
                        c.LastMessageAt,
                        c.CreatedAt
                    );
                }

                return new ConversationDto(
                    c.Id,
                    c.Type.ToString(),
                    c.Name,
                    c.AvatarUrl,
                    c.MemberCount,
                    c.LastMessage,
                    c.LastMessageAt,
                    c.CreatedAt
                );
            }).ToList();
        }
    }
}