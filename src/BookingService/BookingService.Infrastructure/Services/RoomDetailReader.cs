using System.Globalization;
using BookingService.Application.Abtractions.Services;
using Grpc.Core;
using Shared;
using Shared.Grpc.Rooms;

namespace BookingService.Infrastructure.Services;

public sealed class RoomDetailReader(RoomBillingReader.RoomBillingReaderClient client) : IRoomDetailReader
{
    public async Task<RoomDetailSnapshot?> GetRoomDetailAsync(Guid roomId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetRoomBillingInfoAsync(
                new GetRoomBillingInfoRequest { RoomId = roomId.ToString() },
                deadline: DateTime.UtcNow.AddSeconds(5),
                cancellationToken: cancellationToken);

            if (!response.RoomExists)
            {
                return null;
            }

            if (!Guid.TryParse(response.RoomId, out var parsedRoomId) ||
                !Guid.TryParse(response.RoomTypeId, out var parsedRoomTypeId) ||
                !Guid.TryParse(response.BuildingId, out var parsedBuildingId) ||
                !decimal.TryParse(response.BasePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var basePrice))
            {
                throw new ApiException("RoomService returned invalid room detail data.", 502);
            }

            var occupancyPercent = response.Capacity > 0
                ? Math.Round((decimal)response.OccupiedCount / response.Capacity * 100, 2)
                : 0;

            return new RoomDetailSnapshot(
                parsedRoomId,
                response.RoomNumber,
                parsedBuildingId,
                response.BuildingName,
                response.Floor,
                response.Capacity,
                response.OccupiedCount,
                occupancyPercent,
                $"Phòng {response.RoomNumber} thuộc {response.BuildingName}",
                response.Status,
                parsedRoomTypeId,
                response.RoomTypeName,
                basePrice,
                response.Amenities.ToList());
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ApiException(ex.Status.Detail, 400);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new ApiException("RoomService is unavailable. Please try again later.", 503);
        }
        catch (RpcException)
        {
            throw new ApiException("RoomService request failed.", 502);
        }
    }
}
