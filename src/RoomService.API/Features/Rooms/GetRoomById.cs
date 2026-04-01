using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.Rooms
{
    public static class GetRoomById
    {
        public record Query(Guid Id) : IRequest<Response?>;

        public record Response(
            Guid Id,
            string Name,
            Guid BuildingId,
            string BuildingName, // Lấy từ bảng Building
            int Floor,
            int Capacity,
            int OccupiedCount,
            decimal OccupancyPercent,
            string Description,
            RoomStatus RoomStatus,
            Guid RoomTypeId,
            string RoomTypeName, // Lấy từ bảng RoomType
            decimal BasePrice);

        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/rooms/{id:guid}", async (Guid id, IMediator mediator) =>
                {
                    var result = await mediator.Send(new Query(id));

                    if (result is null)
                    {
                        return Results.NotFound(new ApiResponse<string>("Room not found"));
                    }

                    return Results.Ok(new ApiResponse<Response>(result));
                })
                .WithTags("Rooms")
                .WithName("GetRoomById")
                .RequireAuthorization()
                .Produces<Response>(StatusCodes.Status200OK);
            }
        }

        public class Handler(RoomDbContext dbContext) : IRequestHandler<Query, Response?>
        {
            public async Task<Response?> Handle(Query request, CancellationToken cancellationToken)
            {
                var room = await dbContext.Rooms
                    .AsNoTracking() // Tối ưu hiệu năng cho thao tác chỉ đọc (Read-only)
                    .Where(r => r.Id == request.Id)
                    .Select(r => new Response(
                        r.Id,
                        r.RoomNumber, // Ánh xạ cột RoomNumber vào tham số Name của DTO
                        r.BuildingId,
                        r.Building!.Name,
                        r.Floor,
                        r.RoomType!.Capacity,
                        r.OccupiedCount,
                        // Ép kiểu decimal, xử lý chia cho 0 và tính phần trăm
                        r.RoomType.Capacity > 0 
                            ? Math.Round((decimal)r.OccupiedCount / r.RoomType.Capacity * 100, 2) 
                            : 0,
                        $"Phòng {r.RoomNumber} thuộc {r.Building.Name}", // Ghép chuỗi làm Description tạm thời
                        r.RoomStatus,
                        r.RoomTypeId,
                        r.RoomType.Name,
                        r.RoomType.BasePrice
                    ))
                    .FirstOrDefaultAsync(cancellationToken);

                return room;
            }
        }
    }
}