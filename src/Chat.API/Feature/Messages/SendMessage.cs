using Chat.API.Domain.Entities;
using Chat.API.Hubs;
using Chat.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Services;
using System.Security.Claims;

namespace Chat.API.Features.Messages;

public static class SendMessage
{
    public record Response(
        Guid Id,
        Guid ConversationId,
        string SenderId,
        string Content,
        List<string> MediaUrls,
        DateTime CreatedAt
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/conversations/{conversationId}/messages", async (
                Guid conversationId,
                HttpContext httpContext,
                [FromForm] string content,
                IFormFileCollection? files,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                if (string.IsNullOrWhiteSpace(content))
                    return Results.BadRequest(new ApiResponse<string>("Nội dung tin nhắn không được để trống."));

                if (content.Length > 4000)
                    return Results.BadRequest(new ApiResponse<string>("Tin nhắn không được vượt quá 4000 ký tự."));

                if (files != null && files.Count > 10)
                    return Results.BadRequest(new ApiResponse<string>("Tối đa 10 file đính kèm."));

                var accessToken = httpContext.Request.Headers.Authorization
                    .ToString()
                    .Replace("Bearer ", string.Empty);

                var result = await handler.ExecuteAsync(
                    conversationId, userId, content, files, accessToken, ct);

                return Results.Created(
                    $"/api/conversations/{conversationId}/messages/{result.Id}",
                    new ApiResponse<Response>(result));
            })
            .WithTags("Messages")
            .WithName("SendMessage")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<ApiResponse<Response>>(StatusCodes.Status201Created);
        }
    }

    public class Handler(
        ChatDbContext dbContext,
        IMediaService mediaService,
        IProfileService profileService,
        IHubContext<ChatHub> hubContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid conversationId,
            string userId,
            string content,
            IFormFileCollection? files,
            string accessToken,
            CancellationToken ct)
        {
            var isMember = await dbContext.ConversationMembers
                .AnyAsync(m => m.ConversationId == conversationId
                            && m.UserId == userId
                            && !m.IsDeleted, ct);

            if (!isMember)
                throw new ApiException("Bạn không thuộc cuộc trò chuyện này.", StatusCodes.Status403Forbidden);

            var mediaUrls = await UploadMediaAsync(files, mediaService);

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = content,
                MediaUrls = mediaUrls,
            };

            var conversation = await dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

            if (conversation is not null)
                conversation.UpdatedAt = DateTime.UtcNow;

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync(ct);

            // Lấy thông tin người gửi để broadcast
            var profiles = await profileService.GetProfilesAsync([userId], accessToken, ct);
            profiles.TryGetValue(userId, out var senderProfile);

            // Broadcast đến tất cả thành viên trong conversation đang online
            await hubContext.Clients
                .Group(ChatHub.ConversationGroup(conversationId))
                .SendAsync("ReceiveMessage", new MessagePayload(
                    message.Id,
                    message.ConversationId,
                    message.SenderId,
                    senderProfile?.FullName ?? "Người dùng",
                    senderProfile?.AvatarUrl,
                    message.Content,
                    message.MediaUrls,
                    message.CreatedAt
                ), ct);

            return new Response(
                message.Id,
                message.ConversationId,
                message.SenderId,
                message.Content,
                message.MediaUrls,
                message.CreatedAt
            );
        }
    }

    private static async Task<List<string>> UploadMediaAsync(
        IFormFileCollection? files,
        IMediaService mediaService)
    {
        var mediaUrls = new List<string>();
        if (files is null || files.Count == 0) return mediaUrls;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".mp4", ".pdf", ".doc", ".docx" };

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ApiException(
                    $"File '{file.FileName}' có định dạng không hỗ trợ.",
                    StatusCodes.Status400BadRequest);

            if (file.Length > 20 * 1024 * 1024)
                throw new ApiException(
                    $"File '{file.FileName}' quá lớn (tối đa 20MB).",
                    StatusCodes.Status400BadRequest);

            var url = await mediaService.UploadImageAsync(file, "dormitory_chat");
            mediaUrls.Add(url);
        }

        return mediaUrls;
    }
}