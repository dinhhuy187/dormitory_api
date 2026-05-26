using Community.API.Domain.Entities;
using Community.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features.Comments;

public static class CreateComment
{
    public record Request(string Content);

    public record Response(
        Guid Id,
        Guid PostId,
        string AuthorId,
        string Content,
        int LikeCount,
        DateTime CreatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung bình luận không được để trống.")
                .MinimumLength(1).WithMessage("Nội dung phải có ít nhất 1 ký tự.")
                .MaximumLength(2000).WithMessage("Nội dung không được vượt quá 2000 ký tự.");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/posts/{postId}/comments", async (
                Guid postId,
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

                var result = await handler.ExecuteAsync(postId, userId, request, ct);
                return Results.Created(
                    $"/api/posts/{postId}/comments/{result.Id}",
                    new ApiResponse<Response>(result));
            })
            .WithTags("Comments")
            .WithName("CreateComment")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid postId, string userId, Request req, CancellationToken ct)
        {
            var post = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsHidden, ct);

            if (post is null)
                throw new ApiException("Bài viết không tồn tại hoặc đã bị ẩn.", StatusCodes.Status404NotFound);

            var comment = new PostComment
            {
                PostId = postId,
                AuthorId = userId,
                Content = req.Content,
            };

            post.CommentCount++;

            dbContext.PostComments.Add(comment);
            await dbContext.SaveChangesAsync(ct);

            return new Response(
                comment.Id,
                comment.PostId,
                comment.AuthorId,
                comment.Content,
                comment.LikeCount,
                comment.CreatedAt
            );
        }
    }
}