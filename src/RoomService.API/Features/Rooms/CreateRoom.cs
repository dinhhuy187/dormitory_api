using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Entities;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;

namespace RoomService.API.Features.Rooms
{
    public static class CreateRoom
    {
        public record Command(
            Guid BuildingId,
            string Name,
            int Floor,
            Guid RoomTypeId
        );

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.BuildingId).NotEmpty().WithMessage("ID tòa nhà không được để trống.");
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Số phòng không được để trống.")
                    .MaximumLength(20).WithMessage("Số phòng không được vượt quá 20 ký tự.");
                RuleFor(x => x.Floor).GreaterThan(0).WithMessage("Số tầng phải lớn hơn 0.");
                RuleFor(x => x.RoomTypeId).NotEmpty().WithMessage("ID loại phòng không được để trống.");
            }
        }

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/rooms", async ([FromBody] Command command, Handler handler, CancellationToken ct) =>
                {
                    var roomId = await handler.ExecuteAsync(command, ct);
                    
                    // Trả về HTTP 201 Created kèm theo ID của phòng vừa tạo
                    return Results.Created($"/api/rooms/{roomId}", new { Id = roomId });
                })
                .WithTags("Rooms")
                .WithName("CreateRoom")
                .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "SeniorManager"))
                .AddEndpointFilter<ValidationFilter<Command>>()
                .Produces(StatusCodes.Status201Created);
            }
        }

        public class Handler(RoomDbContext dbContext)
        {
            public async Task<Guid> ExecuteAsync(Command request, CancellationToken cancellationToken)
            {
                // --- BƯỚC 1: KIỂM TRA TÒA NHÀ VÀ LOẠI PHÒNG CÓ TỒN TẠI KHÔNG ---
                var buildingExists = await dbContext.Buildings
                    .AnyAsync(b => b.Id == request.BuildingId, cancellationToken);
                if (!buildingExists)
                    throw new ApiException("Không tìm thấy tòa nhà trong hệ thống.", StatusCodes.Status404NotFound);

                var roomTypeExists = await dbContext.RoomTypes
                    .AnyAsync(rt => rt.Id == request.RoomTypeId, cancellationToken);
                if (!roomTypeExists)
                    throw new ApiException("Loại phòng không hợp lệ.", StatusCodes.Status404NotFound);

                // --- BƯỚC 2: KIỂM TRA TRÙNG LẶP SỐ PHÒNG TRONG TÒA NHÀ ---
                var isDuplicate = await dbContext.Rooms.AnyAsync(r =>
                    r.BuildingId == request.BuildingId && 
                    r.RoomNumber == request.Name, cancellationToken);

                if (isDuplicate)
                    throw new ApiException($"Phòng số {request.Name} đã tồn tại trong tòa nhà này.", StatusCodes.Status400BadRequest);

                // --- BƯỚC 3: TẠO ĐỐI TƯỢNG VÀ LƯU DATABASE ---
                var newRoom = new Room
                {
                    Id = Guid.NewGuid(),
                    BuildingId = request.BuildingId,
                    RoomNumber = request.Name,
                    Floor = request.Floor,
                    RoomTypeId = request.RoomTypeId,
                    RoomStatus = RoomStatus.AVAILABLE, // Mặc định phòng mới là sẵn sàng
                    OccupiedCount = 0 // Mặc định chưa có ai ở
                };

                dbContext.Rooms.Add(newRoom);
                await dbContext.SaveChangesAsync(cancellationToken);

                return newRoom.Id;
            }
        }
    }
}