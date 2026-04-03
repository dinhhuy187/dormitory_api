using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.Buildings
{
    public static class GetBuildings
    {
        public record Query : IRequest<ApiResponse<List<Response>>>;
        public record Response(
            Guid Id,
            string ZoneName,
            string Code,
            string Name,
            GenderRestriction GenderRestriction,
            int TotalFloors,
            bool IsActive
            );
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/rooms/buildings", async (IMediator mediator) =>
                {
                    var result = await mediator.Send(new Query());
                    return Results.Ok(result);
                })
                .WithTags("Buildings")
                .WithName("GetBuildings")
                .RequireAuthorization()
                .Produces<List<Response>>(StatusCodes.Status200OK);
            }
        }
        public class Handler(RoomDbContext dbContext) : IRequestHandler<Query, ApiResponse<List<Response>>>
        {
            public async Task<ApiResponse<List<Response>>> Handle(Query request, CancellationToken cancellationToken)
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