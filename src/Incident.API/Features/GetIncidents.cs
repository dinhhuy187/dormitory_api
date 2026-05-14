using Incident.API.Domain.Enums;
using Incident.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Incident.API.Features;

public static class GetIncidents
{
    public record Query(
        Guid? RoomId,
        string? Status,
        int Page = 1,
        int PageSize = 20
    );

    public record IncidentDto(
        Guid Id,
        Guid RoomId,
        string ReporterId,
        Guid CategoryId,
        string CategoryName,
        string Description,
        List<string> ImageUrls,
        string Status,
        DateTime CreatedAt
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/incidents", async (
                [AsParameters] Query query,
                Handler handler,
                CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(query, ct);
                return Results.Ok(result);
            })
            .WithTags("Incidents")
            .WithName("GetIncidents")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Staff"))
            .Produces<List<IncidentDto>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(IncidentDbContext dbContext)
    {
        public async Task<ApiResponse<List<IncidentDto>>> ExecuteAsync(Query request, CancellationToken ct)
        {
            if (request.Page <= 0)
                throw new ApiException("Page phải lớn hơn 0.", StatusCodes.Status400BadRequest);

            if (request.PageSize <= 0 || request.PageSize > 100)
                throw new ApiException("PageSize phải từ 1 đến 100.", StatusCodes.Status400BadRequest);

            if (!string.IsNullOrEmpty(request.Status) &&
                !Enum.TryParse<IncidentStatus>(request.Status, ignoreCase: true, out _))
                throw new ApiException(
                    $"Status không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", Enum.GetNames<IncidentStatus>())}",
                    StatusCodes.Status400BadRequest);

            var query = dbContext.Incidents
                .Include(i => i.Category)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<IncidentStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            {
                query = query.Where(i => i.Status == parsedStatus);
            }

            if (request.RoomId.HasValue)
            {
                query = query.Where(i => i.RoomId == request.RoomId.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(i => new IncidentDto(
                    i.Id,
                    i.RoomId,
                    i.ReporterId,
                    i.CategoryId,
                    i.Category != null ? i.Category.Name : string.Empty,
                    i.Description,
                    i.ImageUrls,
                    i.Status.ToString(),
                    i.CreatedAt
                ))
                .ToListAsync(ct);

            return new ApiResponse<List<IncidentDto>>(items, new PaginationMetadata(
                totalCount,
                request.PageSize,
                request.Page
            ));
        }
    }
}