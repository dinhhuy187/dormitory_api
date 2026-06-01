namespace Billing.API.Infrastructure.Services;

public interface IRoomBillingClient
{
    Task<RoomBillingInfo?> GetRoomBillingInfoAsync(Guid roomId, CancellationToken cancellationToken);
}

public sealed record RoomBillingInfo(
    Guid RoomId,
    string RoomNumber,
    Guid RoomTypeId,
    string RoomTypeName,
    int Capacity,
    int OccupiedCount,
    string Status,
    string BuildingCode,
    int Floor,
    string? BankCode,
    string? AccountNumber,
    string? AccountName);
