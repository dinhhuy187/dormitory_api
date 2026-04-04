using Shared.Endpoints;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.RoomTypes
{
    public static class GetRoomTypes
    {
        public record Response(
            Guid Id,
            string Name,
            int Capacity,
            decimal BasePrice,
            List<string> Amenities
        );
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/rooms/roomtypes", async (Handler handler, CancellationToken ct) =>
                {
                    var result = await handler.ExecuteAsync(ct);
                    return Results.Ok(result);
                })
                .WithTags("Room Types")
                .WithName("GetRoomTypes")
                .RequireAuthorization()
                .Produces<List<Response>>(StatusCodes.Status200OK);
            }
        }
        public class Handler(RoomDbContext dbContext)
        {
            public async Task<ApiResponse<List<Response>>> ExecuteAsync(CancellationToken cancellationToken)
            {
                var roomTypes = await dbContext.RoomTypes
                    .AsNoTracking()
                    .OrderByDescending(rt => rt.Capacity)
                    .Select(rt => new Response(
                        rt.Id,
                        rt.Name, 
                        rt.Capacity,
                        rt.BasePrice,
                        rt.Amenities
                    ))
                    .ToListAsync(cancellationToken);

                return new ApiResponse<List<Response>>(roomTypes);
            }
        }
    }
}