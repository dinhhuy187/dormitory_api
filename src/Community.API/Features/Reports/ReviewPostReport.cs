using Community.API.Domain.Enums;
using Community.API.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Community.API.Features.Reports;

public static class ReviewPostReport
{
    public record Request(string Status);

    public record Response(
        Guid Id,
        Guid PostId,
        string Status
    );

    public class Validator : AbstractValidator<Request>
    {
        private static readonly ReportStatus[] AllowedStatuses =
            [ReportStatus.Reviewed, ReportStatus.Dismissed];

        public Validator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status không được để trống.")
                .Must(s => Enum.TryParse<ReportStatus>(s, ignoreCase: true, out var parsed)
                           && AllowedStatuses.Contains(parsed))
                .WithMessage($"Status không hợp lệ. Giá trị hợp lệ: {string.Join(", ", AllowedStatuses)}");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("api/posts/reports/{reportId}/review", async (
                Guid reportId,
                [FromBody] Request request,
                [FromServices] Handler handler,
                [FromServices] IValidator<Request> validator,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                    return Results.ValidationProblem(validation.ToDictionary());

                var result = await handler.ExecuteAsync(reportId, request, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Reports")
            .WithName("ReviewPostReport")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Staff"))
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid reportId, Request req, CancellationToken ct)
        {
            var report = await dbContext.PostReports
                .FirstOrDefaultAsync(r => r.Id == reportId, ct);

            if (report is null)
                throw new ApiException("Báo cáo không tồn tại.", StatusCodes.Status404NotFound);

            if (report.Status != ReportStatus.Pending)
                throw new ApiException("Báo cáo này đã được xử lý rồi.", StatusCodes.Status400BadRequest);

            report.Status = Enum.Parse<ReportStatus>(req.Status, ignoreCase: true);

            // Nếu duyệt report → ẩn bài viết luôn
            if (report.Status == ReportStatus.Reviewed)
            {
                var post = await dbContext.Posts
                    .FirstOrDefaultAsync(p => p.Id == report.PostId, ct);

                if (post is not null)
                {
                    post.IsHidden = true;
                    post.IsPinned = false;
                    post.UpdatedAt = DateTime.UtcNow;
                }
            }

            await dbContext.SaveChangesAsync(ct);

            return new Response(report.Id, report.PostId, report.Status.ToString());
        }
    }
}