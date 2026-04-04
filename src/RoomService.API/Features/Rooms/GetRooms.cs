using Shared.Endpoints;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.Rooms
{
    public static class GetRooms
    {
        public record Query(
            string? Search,
            Guid? BuildingId,
            RoomStatus? RoomStatus,
            int Page = 1,
            int PageSize = 20
            ) : IRequest<ApiResponse<List<Response>>>;
        
        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0");
                RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("PageSize must be greater than 0")
                    .LessThanOrEqualTo(100).WithMessage("PageSize must be less than or equal to 100");
                RuleFor(x => x.RoomStatus).IsInEnum().When(x => x.RoomStatus.HasValue)
                    .WithMessage("Invalid RoomStatus value ! Allowed values: Available, Full, Maintenance");
                RuleFor(x => x.BuildingId).NotEmpty().When(x => x.BuildingId.HasValue)
                    .WithMessage("BuildingId must be a valid GUID");
            }
        }

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
            decimal BasePrice
        );

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/rooms", async ([AsParameters] Query query, IMediator mediator) =>
                {
                    var result = await mediator.Send(query);
                    return Results.Ok(result);
                })
                .WithTags("Rooms")
                .WithName("GetRooms")
                .RequireAuthorization()
                .Produces<List<Response>>(StatusCodes.Status200OK);
            }
        }

        public class Handler(RoomDbContext dbContext) : IRequestHandler<Query, ApiResponse<List<Response>>>
        {
            public async Task<ApiResponse<List<Response>>> Handle(Query request, CancellationToken cancellationToken)
            {
                var queryable = dbContext.Rooms.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var searchTerm = request.Search.ToLower();
                    queryable = queryable.Where(r => 
                        r.RoomNumber.ToLower().Contains(searchTerm) || 
                        r.Building!.Name.ToLower().Contains(searchTerm) ||
                        r.Building.ZoneName.ToLower().Contains(searchTerm) ||
                        r.RoomType!.Name.ToLower().Contains(searchTerm)
                    );
                }

                if (request.BuildingId.HasValue)
                {
                    queryable = queryable.Where(r => r.BuildingId == request.BuildingId.Value);
                }

                if (request.RoomStatus.HasValue)
                {
                    queryable = queryable.Where(r => r.RoomStatus == request.RoomStatus.Value);
                }

                var totalCount = await queryable.CountAsync(cancellationToken);

                var items = await queryable
                .OrderBy(r => r.Building!.Name) 
                .ThenBy(r => r.RoomNumber) 
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
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
                    .ToListAsync(cancellationToken);


                return new ApiResponse<List<Response>>(items, new PaginationMetadata
                (
                    totalCount,
                    request.PageSize,
                    request.Page
                ));
            }
        }
    }
}