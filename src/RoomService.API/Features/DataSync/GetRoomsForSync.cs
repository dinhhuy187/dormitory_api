using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Database;
using Shared.Endpoints;

namespace RoomService.API.Features.DataSync;

public class GetRoomsForSync
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/rooms/sync", async (RoomDbContext dbContext, CancellationToken ct) =>
            {
                var rooms = await dbContext.Rooms
                    .Include(r => r.RoomType) 
                    .Select(r => new 
                    {
                        r.Id,
                        RoomName = r.RoomNumber, 
                        MonthlyPrice = r.RoomType!.BasePrice,
                        Capacity = r.RoomType.Capacity,
                        OccupiedCount = r.OccupiedCount,
                        Status = r.RoomStatus.ToString() 
                    })
                    .ToListAsync(cancellationToken: ct);
                return Results.Ok(rooms);
            })
            .WithTags("DataSync")
            .WithName("GetRoomsForSync");
        }
    }
}