namespace BookingService.Domain.Services;
public interface IBookingRulesChecker
{
    Task<bool> HasExceededActiveBookingLimitAsync(Guid userId, CancellationToken cancellationToken);
}