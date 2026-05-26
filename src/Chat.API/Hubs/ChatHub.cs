using Chat.API.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Chat.API.Hubs;

[Authorize]
public class ChatHub(ChatDbContext dbContext) : Hub
{

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (userId is null)
        {
            Context.Abort();
            return;
        }

        var conversationIds = await dbContext.ConversationMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && !m.IsDeleted)
            .Select(m => m.ConversationId)
            .ToListAsync();

        foreach (var conversationId in conversationIds)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ConversationGroup(conversationId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (userId is null) return;

        var isMember = await dbContext.ConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId
                        && m.UserId == userId
                        && !m.IsDeleted);

        if (!isMember) return;

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ConversationGroup(conversationId));
    }

    public async Task Typing(Guid conversationId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (userId is null) return;

        // Broadcast đến các thành viên khác trong conversation
        await Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync("UserTyping", new { ConversationId = conversationId, UserId = userId });
    }

    // Client gọi khi dừng gõ
    public async Task StopTyping(Guid conversationId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (userId is null) return;

        await Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync("UserStopTyping", new { ConversationId = conversationId, UserId = userId });
    }

    // Tên group theo convention
    public static string ConversationGroup(Guid conversationId) =>
        $"conversation-{conversationId}";
}