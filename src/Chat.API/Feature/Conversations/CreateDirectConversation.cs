using Chat.API.Domain.Entities;
using Chat.API.Domain.Enums;
using Chat.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Conversations;

public static class CreateDirectConversation
{
    public record Request(string TargetUserId);

    public record Response(
        Guid Id,
        string Type,
        DateTime CreatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TargetUserId)
                .NotEmpty().WithMessage("TargetUserId không được để trống.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/conversations/direct", async (
                [FromBody] Request request,
                HttpContext httpContext,
                [FromServices] Handler handler,
                [FromServices] IValidator<Request> validator,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                    return Results.ValidationProblem(validation.ToDictionary());

                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                if (userId == request.TargetUserId)
                    return Results.BadRequest(new ApiResponse<string>("Không thể tạo cuộc trò chuyện với chính mình."));

                var result = await handler.ExecuteAsync(userId, request, ct);
                return Results.Created($"/api/conversations/{result.Id}", new ApiResponse<Response>(result));
            })
            .WithTags("Conversations")
            .WithName("CreateDirectConversation")
            .RequireAuthorization()
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        }
    }

    public class Handler(ChatDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(string userId, Request req, CancellationToken ct)
        {
            // Kiểm tra đã có conversation Direct giữa 2 người chưa
            var existing = await dbContext.Conversations
                .Where(c => c.Type == ConversationType.Direct)
                .Where(c => c.Members.Any(m => m.UserId == userId && !m.IsDeleted))
                .Where(c => c.Members.Any(m => m.UserId == req.TargetUserId && !m.IsDeleted))
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
                return new Response(existing.Id, existing.Type.ToString(), existing.CreatedAt);

            var conversation = new Conversation
            {
                Type = ConversationType.Direct,
                CreatedBy = userId,
                Members =
                [
                    new ConversationMember { UserId = userId,             Role = MemberRole.Member },
                    new ConversationMember { UserId = req.TargetUserId,   Role = MemberRole.Member },
                ]
            };

            dbContext.Conversations.Add(conversation);
            await dbContext.SaveChangesAsync(ct);

            return new Response(conversation.Id, conversation.Type.ToString(), conversation.CreatedAt);
        }
    }
}