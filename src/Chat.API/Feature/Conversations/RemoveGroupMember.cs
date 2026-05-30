using Chat.API.Domain.Enums;
using Chat.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Conversations;

public static class RemoveGroupMember
{
    public record Request(string MemberId);

    public record Response(
        Guid ConversationId,
        string RemovedMemberId,
        int TotalMemberCount
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.MemberId)
                .NotEmpty().WithMessage("MemberId không được để trống.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/conversations/{conversationId:guid}/members", async (
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
            .WithName("RemoveGroupMember")
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
                throw new ApiException("Chỉ có thể xóa thành viên khỏi nhóm chat.", StatusCodes.Status400BadRequest);

            var requester = conversation.Members
                .FirstOrDefault(m => m.UserId == userId && !m.IsDeleted)
                ?? throw new ApiException("Bạn không phải thành viên của nhóm này.", StatusCodes.Status403Forbidden);

            var isSelfLeave = req.MemberId == userId;

            if (!isSelfLeave && requester.Role != MemberRole.Admin)
                throw new ApiException("Chỉ Admin mới có thể xóa thành viên khác.", StatusCodes.Status403Forbidden);

            var targetMember = conversation.Members
                .FirstOrDefault(m => m.UserId == req.MemberId && !m.IsDeleted)
                ?? throw new ApiException("Thành viên không tồn tại trong nhóm.", StatusCodes.Status404NotFound);

            if (isSelfLeave && requester.Role == MemberRole.Admin)
            {
                var otherActiveMembers = conversation.Members
                    .Where(m => m.UserId != userId && !m.IsDeleted)
                    .ToList();

                if (otherActiveMembers.Count > 0)
                    throw new ApiException(
                        "Admin không được rời khỏi nhóm",
                        StatusCodes.Status400BadRequest
                    );
            }

            targetMember.IsDeleted = true;
            conversation.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            var remainingCount = conversation.Members.Count(m => !m.IsDeleted);

            return new Response(
                conversation.Id,
                req.MemberId,
                remainingCount
            );
        }
    }
}