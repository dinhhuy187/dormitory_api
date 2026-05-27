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
            // 1. Tìm báo cáo cụ thể được chỉ định qua reportId
            var currentReport = await dbContext.PostReports
                .FirstOrDefaultAsync(r => r.Id == reportId, ct);

            if (currentReport is null)
                throw new ApiException("Báo cáo không tồn tại.", StatusCodes.Status404NotFound);

            if (currentReport.Status != ReportStatus.Pending)
                throw new ApiException("Báo cáo này đã được xử lý rồi.", StatusCodes.Status400BadRequest);

            var targetPostId = currentReport.PostId;
            var targetStatus = Enum.Parse<ReportStatus>(req.Status, ignoreCase: true);

            // 2. Lấy TẤT CẢ các báo cáo đang 'Pending' của cùng bài viết đó để xử lý đồng loạt
            var relatedReports = await dbContext.PostReports
                .Where(r => r.PostId == targetPostId && r.Status == ReportStatus.Pending)
                .ToListAsync(ct);

            // 3. Cập nhật trạng thái cho toàn bộ danh sách báo cáo liên quan tìm được
            foreach (var report in relatedReports)
            {
                report.Status = targetStatus;
            }

            // 4. Nếu trạng thái duyệt là Reviewed -> Ẩn bài viết luôn
            if (targetStatus == ReportStatus.Reviewed)
            {
                var post = await dbContext.Posts
                    .FirstOrDefaultAsync(p => p.Id == targetPostId, ct);

                if (post is not null)
                {
                    post.IsHidden = true;
                    post.IsPinned = false;
                    post.UpdatedAt = DateTime.UtcNow;
                }
            }

            // 5. Lưu toàn bộ thay đổi xuống cơ sở dữ liệu
            await dbContext.SaveChangesAsync(ct);

            // Trả về đúng định dạng Response cũ (sử dụng currentReport.Id và targetPostId)
            return new Response(
                currentReport.Id,
                targetPostId,
                targetStatus.ToString()
            );
        }
    }
}