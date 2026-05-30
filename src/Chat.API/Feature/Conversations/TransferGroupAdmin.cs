using Chat.API.Domain.Enums;
using Chat.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Conversations;

public static class TransferGroupAdmin
{
    public record Request(string NewAdminId);

    public record Response(
        Guid ConversationId,
        string NewAdminId,
        string PreviousAdminId
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.NewAdminId)
                .NotEmpty().WithMessage("NewAdminId không được để trống.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/conversations/{conversationId:guid}/admin", async (
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
            .WithName("TransferGroupAdmin")
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
                throw new ApiException("Chỉ có thể chuyển quyền Admin trong nhóm chat.", StatusCodes.Status400BadRequest);

            var currentAdmin = conversation.Members
                .FirstOrDefault(m => m.UserId == userId && !m.IsDeleted)
                ?? throw new ApiException("Bạn không phải thành viên của nhóm này.", StatusCodes.Status403Forbidden);

            if (currentAdmin.Role != MemberRole.Admin)
                throw new ApiException("Chỉ Admin mới có thể chuyển quyền.", StatusCodes.Status403Forbidden);

            if (req.NewAdminId == userId)
                throw new ApiException("Bạn đã là Admin của nhóm này.", StatusCodes.Status400BadRequest);

            var newAdmin = conversation.Members
                .FirstOrDefault(m => m.UserId == req.NewAdminId && !m.IsDeleted)
                ?? throw new ApiException("Thành viên không tồn tại trong nhóm.", StatusCodes.Status404NotFound);

            currentAdmin.Role = MemberRole.Member;
            newAdmin.Role = MemberRole.Admin;
            conversation.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return new Response(
                conversation.Id,
                newAdmin.UserId,
                currentAdmin.UserId
            );
        }
    }
}