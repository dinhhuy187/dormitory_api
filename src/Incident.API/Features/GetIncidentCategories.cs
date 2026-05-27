using Incident.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Incident.API.Features;

public static class GetIncidentCategories
{
    public record CategoryDto(
        Guid Id,
        string Name
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/incidents/categories", async (
                Handler handler,
                CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(ct);
                return Results.Ok(result);
            })
            .WithTags("Incident Categories")
            .WithName("GetIncidentCategories")
            .AllowAnonymous()
            .Produces<ApiResponse<List<CategoryDto>>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(IncidentDbContext dbContext)
    {
        public async Task<ApiResponse<List<CategoryDto>>> ExecuteAsync(CancellationToken ct)
        {
            var items = await dbContext.IncidentCategories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name))
                .ToListAsync(ct);

            return new ApiResponse<List<CategoryDto>>(items);
        }
    }
}