using Community.API.Domain.Enums;
using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Community.API.Features.Reports;

public static class GetPostReports
{
    public record Query(
        string? Status,
        int Page = 1,
        int PageSize = 20
    );

    public record ReportDto(
        Guid Id,
        Guid PostId,
        string ReporterId,
        string Reason,
        string? Note,
        string Status,
        DateTime CreatedAt
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/posts/reports", async (
                [AsParameters] Query query,
                Handler handler,
                CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(query, ct);
                return Results.Ok(result);
            })
            .WithTags("Reports")
            .WithName("GetPostReports")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Staff"))
            .Produces<ApiResponse<List<ReportDto>>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<ApiResponse<List<ReportDto>>> ExecuteAsync(Query request, CancellationToken ct)
        {
            if (request.Page <= 0)
                throw new ApiException("Page phải lớn hơn 0.", StatusCodes.Status400BadRequest);

            if (request.PageSize <= 0 || request.PageSize > 100)
                throw new ApiException("PageSize phải từ 1 đến 100.", StatusCodes.Status400BadRequest);

            if (!string.IsNullOrEmpty(request.Status) &&
                !Enum.TryParse<ReportStatus>(request.Status, ignoreCase: true, out _))
                throw new ApiException(
                    $"Status không hợp lệ. Giá trị hợp lệ: {string.Join(", ", Enum.GetNames<ReportStatus>())}",
                    StatusCodes.Status400BadRequest);

            var query = dbContext.PostReports
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<ReportStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new ReportDto(
                    r.Id,
                    r.PostId,
                    r.ReporterId,
                    r.Reason.ToString(),
                    r.Note,
                    r.Status.ToString(),
                    r.CreatedAt
                ))
                .ToListAsync(ct);

            return new ApiResponse<List<ReportDto>>(items, new PaginationMetadata(
                totalCount,
                request.PageSize,
                request.Page
            ));
        }
    }
}