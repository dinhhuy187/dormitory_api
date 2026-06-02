using Grpc.Core;
using Shared;
using Shared.Grpc.Rooms;

namespace Billing.API.Infrastructure.Services;

public sealed class RoomBillingClient(RoomBillingReader.RoomBillingReaderClient client) : IRoomBillingClient
{
    public async Task<RoomBillingInfo?> GetRoomBillingInfoAsync(Guid roomId, CancellationToken cancellationToken)
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
                !Guid.TryParse(response.RoomTypeId, out var parsedRoomTypeId))
            {
                throw new ApiException("RoomService returned invalid room billing data.", StatusCodes.Status502BadGateway);
            }

            if (string.IsNullOrWhiteSpace(response.BuildingCode) || response.Floor <= 0)
            {
                throw new ApiException("RoomService returned invalid room location data.", StatusCodes.Status502BadGateway);
            }

            return new RoomBillingInfo(
                parsedRoomId,
                response.RoomNumber,
                parsedRoomTypeId,
                response.RoomTypeName,
                response.Capacity,
                response.OccupiedCount,
                response.Status,
                response.BuildingCode.Trim(),
                response.Floor,
                Normalize(response.BankCode),
                Normalize(response.AccountNumber),
                Normalize(response.AccountName));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ApiException(ex.Status.Detail, StatusCodes.Status400BadRequest);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new ApiException("RoomService is unavailable. Please try again later.", StatusCodes.Status503ServiceUnavailable);
        }
        catch (RpcException)
        {
            throw new ApiException("RoomService request failed.", StatusCodes.Status502BadGateway);
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
