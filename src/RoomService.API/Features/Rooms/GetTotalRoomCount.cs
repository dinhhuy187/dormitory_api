using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.Rooms
{
    public static class GetTotalRoomCount
    {
        public record Query(Guid? BuildingId) : IRequest<ApiResponse<Dictionary<string, int>>>;
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/rooms/count", async ([AsParameters] Query query, IMediator mediator) =>
                {
                    var result = await mediator.Send(query);
                    return Results.Ok(result);
                })
                .WithTags("Rooms")
                .WithName("GetTotalRoomCount")
                .Produces<Dictionary<string, int>>(StatusCodes.Status200OK);
            }
        }
        public class Handler(RoomDbContext dbContext) : IRequestHandler<Query, ApiResponse<Dictionary<string, int>>>
        {
            public async Task<ApiResponse<Dictionary<string, int>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var queryable = dbContext.Rooms.AsNoTracking().AsQueryable();

                if (request.BuildingId.HasValue)
                {
                    queryable = queryable.Where(r => r.BuildingId == request.BuildingId.Value);
                }

                var dbCounts = await queryable
                    .GroupBy(r => r.RoomStatus)
                    .Select(g => new 
                    { 
                        Status = g.Key, 
                        Count = g.Count() 
                    })
                    .ToListAsync(cancellationToken);

                // Khởi tạo Dictionary kết quả
                var result = new Dictionary<string, int>();

                // Lấy tất cả các giá trị có thể có của Enum RoomStatus (Available, Full, Maintenance...)
                var allStatuses = Enum.GetValues<RoomStatus>();

                // Ghép dữ liệu từ DB vào mảng Enum để đảm bảo luôn trả về đủ Key (Gắn số 0 cho các trạng thái không có phòng)
                foreach (var status in allStatuses)
                {
                    var found = dbCounts.FirstOrDefault(x => x.Status == status);
                    
                    // Key là tên Enum dưới dạng chuỗi, Value là số lượng
                    result[status.ToString()] = found?.Count ?? 0;
                }
                result["Total"] = result.Values.Sum(); // Thêm tổng số phòng vào kết quả

                return new ApiResponse<Dictionary<string, int>>(result);
            }
        }
    }
}