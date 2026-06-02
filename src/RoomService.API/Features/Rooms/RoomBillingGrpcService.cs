using System.Globalization;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Infrastructure.Database;
using Shared.Grpc.Rooms;

namespace RoomService.API.Features.Rooms;

public sealed class RoomBillingGrpcService(RoomDbContext dbContext) : RoomBillingReader.RoomBillingReaderBase
{
    public override async Task<GetRoomBillingInfoResponse> GetRoomBillingInfo(
        GetRoomBillingInfoRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.RoomId, out var roomId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "room_id must be a valid GUID."));
        }

        var room = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new
            {
                r.Id,
                r.RoomNumber,
                r.RoomTypeId,
                RoomTypeName = r.RoomType!.Name,
                r.RoomType.Capacity,
                r.RoomType.BasePrice,
                r.RoomType.Amenities,
                r.OccupiedCount,
                Status = r.RoomStatus.ToString(),
                BuildingId = r.Building!.Id,
                BuildingCode = r.Building!.Code,
                BuildingName = r.Building!.Name,
                r.Building.BankCode,
                r.Building.AccountNumber,
                r.Building.AccountName,
                r.Floor
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        if (room is null)
        {
            return new GetRoomBillingInfoResponse
            {
                RoomId = request.RoomId,
                RoomExists = false
            };
        }

        var response = new GetRoomBillingInfoResponse
        {
            RoomId = room.Id.ToString(),
            RoomExists = true,
            RoomNumber = room.RoomNumber,
            RoomTypeId = room.RoomTypeId.ToString(),
            RoomTypeName = room.RoomTypeName,
            Capacity = room.Capacity,
            OccupiedCount = room.OccupiedCount,
            Status = room.Status,
            BuildingId = room.BuildingId.ToString(),
            BuildingCode = room.BuildingCode,
            BuildingName = room.BuildingName,
            Floor = room.Floor,
            BankCode = room.BankCode ?? string.Empty,
            AccountNumber = room.AccountNumber ?? string.Empty,
            AccountName = room.AccountName ?? string.Empty,
            BasePrice = room.BasePrice.ToString(CultureInfo.InvariantCulture)
        };

        response.Amenities.AddRange(room.Amenities);
        return response;
    }
}
