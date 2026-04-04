using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;

namespace RoomService.API.Features.Buildings
{
    public static class GetBuildings
    {
        public record Response(
            Guid Id,
            string ZoneName,
            string Code,
            string Name,
            GenderRestriction GenderRestriction,
            int TotalFloors,
            bool IsActive
            );
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/rooms/buildings", async (Handler handler, CancellationToken ct) =>
                {
                    var result = await handler.ExecuteAsync(ct);
                    return Results.Ok(result);
                })
                .WithTags("Buildings")
                .WithName("GetBuildings")
                .RequireAuthorization()
                .Produces<List<Response>>(StatusCodes.Status200OK);
            }
        }
        public class Handler(RoomDbContext dbContext)
        {
            public async Task<ApiResponse<List<Response>>> ExecuteAsync(CancellationToken cancellationToken)
            {
                var buildings = await dbContext.Buildings
                    .AsNoTracking()
                    .Select(b => new Response(b.Id, b.ZoneName, b.Code, b.Name, b.GenderRestriction, b.TotalFloors, b.IsActive))
                    .ToListAsync(cancellationToken);

                return new ApiResponse<List<Response>>(buildings);
            }
        }
    }
}