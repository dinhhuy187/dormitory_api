using Community.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features.Comments;

public static class UpdateComment
{
    public record Request(string Content);

    public record Response(
        Guid Id,
        string Content,
        DateTime UpdatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung bình luận không được để trống.")
                .MaximumLength(2000).WithMessage("Nội dung không được vượt quá 2000 ký tự.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/posts/{postId}/comments/{commentId}", async (
                Guid postId,
                Guid commentId,
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

                var result = await handler.ExecuteAsync(postId, commentId, userId, request, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Comments")
            .WithName("UpdateComment")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid postId, Guid commentId, string userId, Request req, CancellationToken ct)
        {
            var comment = await dbContext.PostComments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.PostId == postId, ct);

            if (comment is null)
                throw new ApiException("Bình luận không tồn tại.", StatusCodes.Status404NotFound);

            if (comment.IsHidden)
                throw new ApiException("Bình luận đã bị ẩn, không thể chỉnh sửa.", StatusCodes.Status400BadRequest);

            // Chỉ chủ comment mới được sửa
            if (comment.AuthorId != userId)
                throw new ApiException("Bạn không có quyền chỉnh sửa bình luận này.", StatusCodes.Status403Forbidden);

            comment.Content = req.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return new Response(comment.Id, comment.Content, comment.UpdatedAt);
        }
    }
}