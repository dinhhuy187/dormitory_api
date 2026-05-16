using Community.API.Domain.Entities;
using Community.API.Domain.Enums;
using Community.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features.Reports;

public static class CreatePostReport
{
    public record Request(ReportReason Reason, string? Note);

    public record Response(
        Guid Id,
        Guid PostId,
        string ReporterId,
        string Reason,
        string? Note,
        string Status,
        DateTime CreatedAt
    );

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Reason)
                .IsInEnum()
                .WithMessage($"Lý do không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", Enum.GetNames<ReportReason>())}");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .WithMessage("Ghi chú không được vượt quá 500 ký tự.")
                .When(x => x.Note is not null);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/posts/{postId}/reports", async (
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
                    $"/api/posts/{postId}/reports/{result.Id}",
                    new ApiResponse<Response>(result));
            })
            .WithTags("Reports")
            .WithName("CreatePostReport")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(
            Guid postId, string userId, Request req, CancellationToken ct)
        {
            var post = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsHidden, ct);

            if (post is null)
                throw new ApiException("Bài viết không tồn tại hoặc đã bị ẩn.", StatusCodes.Status404NotFound);

            // Không tự report bài của mình
            if (post.AuthorId == userId)
                throw new ApiException("Bạn không thể báo cáo bài viết của chính mình.", StatusCodes.Status400BadRequest);

            // 1 user chỉ report 1 post 1 lần
            var alreadyReported = await dbContext.PostReports
                .AnyAsync(r => r.PostId == postId && r.ReporterId == userId, ct);

            if (alreadyReported)
                throw new ApiException("Bạn đã báo cáo bài viết này rồi.", StatusCodes.Status400BadRequest);

            var report = new PostReport
            {
                PostId = postId,
                ReporterId = userId,
                Reason = req.Reason,
                Note = req.Note,
            };

            dbContext.PostReports.Add(report);
            await dbContext.SaveChangesAsync(ct);

            return new Response(
                report.Id,
                report.PostId,
                report.ReporterId,
                report.Reason.ToString(),
                report.Note,
                report.Status.ToString(),
                report.CreatedAt
            );
        }
    }
}