namespace BookingService.Domain.Services;

public interface IRegistrationPeriodChecker
{
    Task<bool> IsRegistrationPortalOpenAsync(string termName, CancellationToken cancellationToken);
}