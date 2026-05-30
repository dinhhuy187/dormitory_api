using Chat.API.Domain.Enums;
using Chat.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Services;
using System.Security.Claims;

namespace Chat.API.Features.Conversations;

public static class GetConversationMembers
{
    public record MemberDto(
        string UserId,
        string FullName,
        string? AvatarUrl,
        string Role,
        DateTime JoinedAt
    );

    public record Response(
        Guid ConversationId,
        int TotalMemberCount,
        List<MemberDto> Members
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/conversations/{conversationId:guid}/members", async (
                Guid conversationId,
                HttpContext httpContext,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var accessToken = httpContext.Request.Headers.Authorization
                    .ToString()
                    .Replace("Bearer ", string.Empty);

                var result = await handler.ExecuteAsync(userId, conversationId, accessToken, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Conversations")
            .WithName("GetConversationMembers")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(ChatDbContext dbContext, IProfileService profileService)
    {
        public async Task<Response> ExecuteAsync(
            string userId,
            Guid conversationId,
            string accessToken,
            CancellationToken ct)
        {
            var conversation = await dbContext.Conversations
                .AsNoTracking()
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct)
                ?? throw new ApiException("Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);

            // Phải là thành viên còn trong nhóm mới được xem
            var isMember = conversation.Members
                .Any(m => m.UserId == userId && !m.IsDeleted);

            if (!isMember)
                throw new ApiException("Bạn không phải thành viên của cuộc trò chuyện này.", StatusCodes.Status403Forbidden);

            var activeMembers = conversation.Members
                .Where(m => !m.IsDeleted)
                .ToList();

            var memberIds = activeMembers.Select(m => m.UserId).Distinct();

            var profiles = await profileService.GetProfilesAsync(memberIds, accessToken, ct);

            var memberDtos = activeMembers
                .Select(m =>
                {
                    profiles.TryGetValue(m.UserId, out var profile);
                    return new MemberDto(
                        m.UserId,
                        profile?.FullName ?? "Người dùng",
                        profile?.AvatarUrl,
                        m.Role.ToString(),
                        m.JoinedAt
                    );
                })
                .OrderByDescending(m => m.Role == "Admin")   // Admin lên đầu
                .ThenBy(m => m.FullName)
                .ToList();

            return new Response(
                conversationId,
                memberDtos.Count,
                memberDtos
            );
        }
    }
}