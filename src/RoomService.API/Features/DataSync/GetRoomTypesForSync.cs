using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Database;
using Shared.Endpoints;

namespace RoomService.API.Features.DataSync;

public static class GetRoomTypesForSync
{
    public sealed record RoomTypeSyncDto(
        Guid Id,
        string Name,
        int Capacity,
        decimal BasePrice,
        List<string> Amenities);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/rooms/roomtypes/sync", async (RoomDbContext dbContext, CancellationToken ct) =>
                {
                    var roomTypes = await dbContext.RoomTypes
                        .AsNoTracking()
                        .OrderByDescending(roomType => roomType.Capacity)
                        .ThenBy(roomType => roomType.BasePrice)
                        .Select(roomType => new RoomTypeSyncDto(
                            roomType.Id,
                            roomType.Name,
                            roomType.Capacity,
                            roomType.BasePrice,
                            roomType.Amenities))
                        .ToListAsync(ct);

                    return Results.Ok(roomTypes);
                })
                .WithTags("DataSync")
                .WithName("GetRoomTypesForSync")
                .WithDescription("Internal sync endpoint. Returns room type reference data with real RoomService IDs for service-to-service seed and sync jobs. No user JWT is required.")
                .Produces<List<RoomTypeSyncDto>>(StatusCodes.Status200OK);
        }
    }
}
