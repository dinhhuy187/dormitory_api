using Shared.Endpoints;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.RoomTypes
{
    public static class GetRoomTypes
    {
        public record Query : IRequest<ApiResponse<List<Response>>>;
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
                app.MapGet("/api/rooms/roomtypes", async (IMediator mediator) =>
                {
                    var result = await mediator.Send(new Query());
                    return Results.Ok(result);
                })
                .WithTags("Room Types")
                .WithName("GetRoomTypes")
                .RequireAuthorization()
                .Produces<List<Response>>(StatusCodes.Status200OK);
            }
        }
        public class Handler(RoomDbContext dbContext) : IRequestHandler<Query, ApiResponse<List<Response>>>
        {
            public async Task<ApiResponse<List<Response>>> Handle(Query request, CancellationToken cancellationToken)
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