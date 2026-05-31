using Chat.API.Domain.Enums;
using Chat.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Conversations;

public static class RenameGroupConversation
{
    public record Request(string Name);

    public record Response(
        Guid ConversationId,
        string Name,
        DateTime UpdatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên nhóm không được để trống.")
                .MaximumLength(100).WithMessage("Tên nhóm không được vượt quá 100 ký tự.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/conversations/{conversationId:guid}/name", async (
                Guid conversationId,
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

                var result = await handler.ExecuteAsync(userId, conversationId, request, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Conversations")
            .WithName("RenameGroupConversation")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
        }
    }

    public class Handler(ChatDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(
            string userId,
            Guid conversationId,
            Request req,
            CancellationToken ct)
        {
            var conversation = await dbContext.Conversations
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct)
                ?? throw new ApiException("Cuộc trò chuyện không tồn tại.", StatusCodes.Status404NotFound);

            if (conversation.Type != ConversationType.Group)
                throw new ApiException("Chỉ có thể đổi tên nhóm chat.", StatusCodes.Status400BadRequest);

            var requester = conversation.Members
                .FirstOrDefault(m => m.UserId == userId && !m.IsDeleted)
                ?? throw new ApiException("Bạn không phải thành viên của nhóm này.", StatusCodes.Status403Forbidden);

            if (conversation.Name == req.Name)
                throw new ApiException("Tên nhóm mới phải khác tên hiện tại.", StatusCodes.Status400BadRequest);

            conversation.Name = req.Name;
            conversation.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return new Response(
                conversation.Id,
                conversation.Name,
                conversation.UpdatedAt
            );
        }
    }
}