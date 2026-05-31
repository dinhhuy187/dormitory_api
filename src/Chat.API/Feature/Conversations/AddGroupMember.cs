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

public static class AddGroupMembers
{
    public record Request(List<string> MemberIds);

    public record Response(
        Guid ConversationId,
        List<string> AddedMemberIds,
        int TotalMemberCount
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.MemberIds)
                .NotEmpty().WithMessage("Phải có ít nhất 1 thành viên cần thêm.")
                .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Danh sách thành viên bị trùng.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/conversations/{conversationId:guid}/members", async (
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
            .WithName("AddGroupMembers")
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
                throw new ApiException("Chỉ có thể thêm thành viên vào nhóm.", StatusCodes.Status400BadRequest);

            var requester = conversation.Members.FirstOrDefault(m => m.UserId == userId && !m.IsDeleted)
                ?? throw new ApiException("Bạn không phải thành viên của nhóm này.", StatusCodes.Status403Forbidden);

            if (requester.Role != MemberRole.Admin)
                throw new ApiException("Chỉ Admin mới có thể thêm thành viên.", StatusCodes.Status403Forbidden);

            var existingMemberIds = conversation.Members
                .Where(m => !m.IsDeleted)
                .Select(m => m.UserId)
                .ToHashSet();

            var newMemberIds = req.MemberIds
                .Where(id => !existingMemberIds.Contains(id))
                .Distinct()
                .ToList();

            if (existingMemberIds.Count + newMemberIds.Count > 100)
                throw new ApiException("Nhóm tối đa 100 thành viên.", StatusCodes.Status400BadRequest);

            if (newMemberIds.Count == 0)
                throw new ApiException("Tất cả thành viên đã có trong nhóm.", StatusCodes.Status400BadRequest);

            var newMembers = newMemberIds
                .Select(id => new ConversationMember
                {
                    ConversationId = conversationId,
                    UserId = id,
                    Role = MemberRole.Member
                })
                .ToList();

            dbContext.ConversationMembers.AddRange(newMembers);
            await dbContext.SaveChangesAsync(ct);

            return new Response(
                conversationId,
                newMemberIds,
                existingMemberIds.Count + newMemberIds.Count
            );
        }
    }
}