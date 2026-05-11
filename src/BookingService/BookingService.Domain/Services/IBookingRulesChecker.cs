namespace BookingService.Domain.Services;
public interface IBookingRulesChecker
{
    Task<bool> HasExceededActiveBookingLimitAsync(Guid userId,string termName, CancellationToken cancellationToken);
    Task<bool> IsRoomAvailableForBookingAsync(Guid roomId, CancellationToken cancellationToken);
}