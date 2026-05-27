using Chat.API.Hubs;
using Chat.API.Infrastructure.Database;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Messages;

public static class DeleteMessage
{
    public record Response(Guid Id, bool IsDeleted);

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/conversations/{conversationId}/messages/{messageId}", async (
                Guid conversationId,
                Guid messageId,
                HttpContext httpContext,
                Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                var isAdminOrStaff = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Staff");

                var result = await handler.ExecuteAsync(
                    conversationId, messageId, userId, isAdminOrStaff, ct);

                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Messages")
            .WithName("DeleteMessage")
            .RequireAuthorization()
            .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public class Handler(ChatDbContext dbContext, IHubContext<ChatHub> hubContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid conversationId, Guid messageId,
            string userId, bool isAdminOrStaff,
            CancellationToken ct)
        {
            var message = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId
                                       && m.ConversationId == conversationId, ct);

            if (message is null)
                throw new ApiException("Tin nhắn không tồn tại.", StatusCodes.Status404NotFound);

            if (message.IsDeleted)
                throw new ApiException("Tin nhắn đã bị xóa rồi.", StatusCodes.Status400BadRequest);

            if (message.SenderId != userId && !isAdminOrStaff)
                throw new ApiException("Bạn không có quyền xóa tin nhắn này.", StatusCodes.Status403Forbidden);

            message.IsDeleted = true;
            await dbContext.SaveChangesAsync(ct);

            // Broadcast xóa tin nhắn
            await hubContext.Clients
                .Group(ChatHub.ConversationGroup(conversationId))
                .SendAsync("MessageDeleted", new MessageDeletedPayload(
                    message.Id,
                    conversationId
                ), ct);

            return new Response(message.Id, message.IsDeleted);
        }
    }
}