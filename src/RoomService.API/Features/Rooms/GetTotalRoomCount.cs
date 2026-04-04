using Shared.Endpoints;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.Rooms
{
    public static class GetTotalRoomCount
    {
        public record Query(Guid? BuildingId);
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/rooms/count", async ([AsParameters] Query query, Handler handler, CancellationToken ct) =>
                {
                    var result = await handler.ExecuteAsync(query, ct);
                    return Results.Ok(result);
                })
                .WithTags("Rooms")
                .WithName("GetTotalRoomCount")
                .Produces<Dictionary<string, int>>(StatusCodes.Status200OK);
            }
        }
        public class Handler(RoomDbContext dbContext)
        {
            public async Task<ApiResponse<Dictionary<string, int>>> ExecuteAsync(Query request, CancellationToken cancellationToken)
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