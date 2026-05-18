using Chat.API.Domain.Entities;
using Chat.API.Domain.Enums;
using Chat.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Chat.API.Features.Conversations;

public static class CreateGroupConversation
{
    public record Request(string Name, List<string> MemberIds);

    public record Response(
        Guid Id,
        string Type,
        string Name,
        int MemberCount,
        DateTime CreatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên nhóm không được để trống.")
                .MaximumLength(100).WithMessage("Tên nhóm không được vượt quá 100 ký tự.");

            RuleFor(x => x.MemberIds)
                .NotEmpty().WithMessage("Nhóm phải có ít nhất 1 thành viên khác.")
                .Must(ids => ids.Count <= 99).WithMessage("Nhóm tối đa 100 thành viên.")
                .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Danh sách thành viên bị trùng.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/conversations/group", async (
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

                var result = await handler.ExecuteAsync(userId, request, ct);
                return Results.Created($"/api/conversations/{result.Id}", new ApiResponse<Response>(result));
            })
            .WithTags("Conversations")
            .WithName("CreateGroupConversation")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        }
    }

    public class Handler(ChatDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(string userId, Request req, CancellationToken ct)
        {
            // Loại bỏ trường hợp tự thêm mình
            var memberIds = req.MemberIds
                .Where(id => id != userId)
                .Distinct()
                .ToList();

            var members = memberIds
                .Select(id => new ConversationMember { UserId = id, Role = MemberRole.Member })
                .ToList();

            // Người tạo là Admin
            members.Add(new ConversationMember { UserId = userId, Role = MemberRole.Admin });

            var conversation = new Conversation
            {
                Type = ConversationType.Group,
                Name = req.Name,
                CreatedBy = userId,
                Members = members,
            };

            dbContext.Conversations.Add(conversation);
            await dbContext.SaveChangesAsync(ct);

            return new Response(
                conversation.Id,
                conversation.Type.ToString(),
                conversation.Name!,
                conversation.Members.Count,
                conversation.CreatedAt
            );
        }
    }
}