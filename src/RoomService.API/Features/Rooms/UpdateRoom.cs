using Shared.Endpoints;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Enum;
using RoomService.API.Infrastructure.Database;
using Shared;

namespace RoomService.API.Features.Rooms
{
    public static class UpdateRoom
    {
        public record RequestBody(
            Guid BuildingId,
            string Name,
            int Floor,
            Guid RoomTypeId,
            RoomStatus RoomStatus
        );

        public record Command(Guid Id, RequestBody Body);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Id).NotEmpty().WithMessage("ID phòng không hợp lệ.");
                
                RuleFor(x => x.Body.BuildingId).NotEmpty().WithMessage("ID tòa nhà không hợp lệ.");
                
                RuleFor(x => x.Body.Name)
                    .NotEmpty().WithMessage("Số phòng không được để trống.")
                    .MaximumLength(20).WithMessage("Số phòng không được vượt quá 20 ký tự.");
                
                RuleFor(x => x.Body.Floor).GreaterThan(0).WithMessage("Số tầng phải lớn hơn 0.");
                
                RuleFor(x => x.Body.RoomTypeId).NotEmpty().WithMessage("ID loại phòng không hợp lệ.");
                
                RuleFor(x => x.Body.RoomStatus).IsInEnum().WithMessage("Trạng thái phòng không hợp lệ.");
            }
        }

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPut("/api/rooms/{id:guid}", async (Guid id, [FromBody] RequestBody body, Handler handler, CancellationToken ct) =>
                {
                    var command = new Command(id, body);
                    var result = await handler.ExecuteAsync(command, ct);

                    if (!result) return Results.NotFound(new { Message = "Không tìm thấy phòng để cập nhật." });

                    return Results.NoContent(); 
                })
                .WithTags("Rooms")
                .WithName("UpdateRoom")
                .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "SeniorManager"))
                .Produces(StatusCodes.Status204NoContent);
            }
        }

        public class Handler(RoomDbContext dbContext)
        {
            public async Task<bool> ExecuteAsync(Command request, CancellationToken cancellationToken)
            {
                var room = await dbContext.Rooms
                    .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

                if (room is null) return false;

                if (room.BuildingId != request.Body.BuildingId)
                {
                    var buildingExists = await dbContext.Buildings.AnyAsync(b => b.Id == request.Body.BuildingId, cancellationToken);
                    if (!buildingExists)
                    {
                        throw new ApiException("Không tìm thấy tòa nhà đích trong hệ thống.", StatusCodes.Status404NotFound);
                    }
                }

                // Chạy kiểm tra nếu Số phòng thay đổi HOẶC Tòa nhà thay đổi
                if (room.RoomNumber != request.Body.Name || room.BuildingId != request.Body.BuildingId)
                {
                    var isDuplicate = await dbContext.Rooms.AnyAsync(r =>
                        r.BuildingId == request.Body.BuildingId && // Phải quét theo BuildingId từ request (Tòa nhà đích)
                        r.RoomNumber == request.Body.Name && 
                        r.Id != room.Id, cancellationToken);

                    if (isDuplicate)
                    {
                        throw new ApiException($"Số phòng '{request.Body.Name}' đã tồn tại trong tòa nhà này.", StatusCodes.Status400BadRequest);
                    }
                }

                // --- KIỂM TRA NGHIỆP VỤ 3: SỨC CHỨA CỦA LOẠI PHÒNG MỚI ---
                if (room.RoomTypeId != request.Body.RoomTypeId)
                {
                    var newRoomType = await dbContext.RoomTypes.FindAsync([request.Body.RoomTypeId], cancellationToken);

                    if (newRoomType is null)
                    {
                        throw new ApiException("Không tìm thấy loại phòng hệ thống.", StatusCodes.Status404NotFound);
                    }

                    if (newRoomType.Capacity < room.OccupiedCount)
                    {
                        throw new ApiException($"Không thể đổi sang loại phòng này. Sức chứa mới ({newRoomType.Capacity}) nhỏ hơn số người đang ở hiện tại ({room.OccupiedCount}).", StatusCodes.Status400BadRequest);
                    }
                }

                // Cập nhật toàn bộ dữ liệu
                room.BuildingId = request.Body.BuildingId;
                room.RoomNumber = request.Body.Name;
                room.Floor = request.Body.Floor;
                room.RoomTypeId = request.Body.RoomTypeId;
                room.RoomStatus = request.Body.RoomStatus;

                await dbContext.SaveChangesAsync(cancellationToken);

                return true;
            }
        }
    }
}